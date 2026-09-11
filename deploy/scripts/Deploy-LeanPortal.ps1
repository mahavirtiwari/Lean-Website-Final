<#
.SYNOPSIS
    Builds and deploys the MSME Competitive (LEAN) Scheme portal to IIS on Windows Server.

.DESCRIPTION
    Publishes the ASP.NET Core API and the Angular front end, stops the target IIS
    application pools, copies the output into place, and starts them again.

    The uploads folder and appsettings.Production.json are preserved across
    deployments: uploads hold live content, and the production settings file
    holds environment secrets that are not in source control.

.PARAMETER SiteRoot
    Physical root of the IIS site, e.g. C:\inetpub\LeanPortal.
    The API is deployed to <SiteRoot>\api and the front end to <SiteRoot>.

.PARAMETER AppPoolName
    Application pool serving the site.

.PARAMETER ApiAppPoolName
    Application pool serving the /api sub-application. Defaults to AppPoolName.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.PARAMETER SkipBuild
    Deploy the existing publish output without rebuilding.

.EXAMPLE
    .\Deploy-LeanPortal.ps1 -SiteRoot C:\inetpub\LeanPortal -AppPoolName LeanPortal

.NOTES
    Run from an elevated PowerShell session on the web server, or from a build
    agent with permission to the site root and to IIS.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string] $SiteRoot,

    [Parameter(Mandatory = $true)]
    [string] $AppPoolName,

    [string] $ApiAppPoolName,

    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',

    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not $ApiAppPoolName) { $ApiAppPoolName = $AppPoolName }

$repoRoot    = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$apiProject  = Join-Path $repoRoot 'backend\src\LeanPortal.Api\LeanPortal.Api.csproj'
$webProject  = Join-Path $repoRoot 'frontend\lean-portal'
$stagingRoot = Join-Path $env:TEMP "lean-portal-publish-$(Get-Date -Format yyyyMMddHHmmss)"

$apiStaging = Join-Path $stagingRoot 'api'
$webStaging = Join-Path $stagingRoot 'web'

$apiTarget = Join-Path $SiteRoot 'api'
$webTarget = $SiteRoot

function Write-Step([string] $Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

# ---------------------------------------------------------------- pre-flight --

Write-Step 'Checking prerequisites'

foreach ($tool in 'dotnet', 'npm') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "$tool was not found on PATH. Install it before deploying."
    }
}

if (-not (Test-Path $SiteRoot)) {
    throw "Site root '$SiteRoot' does not exist. Create the IIS site first."
}

Import-Module WebAdministration -ErrorAction Stop

foreach ($pool in @($AppPoolName, $ApiAppPoolName | Select-Object -Unique)) {
    if (-not (Test-Path "IIS:\AppPools\$pool")) {
        throw "Application pool '$pool' does not exist."
    }
}

Write-Host "  Repository : $repoRoot"
Write-Host "  Site root  : $SiteRoot"
Write-Host "  App pools  : $AppPoolName, $ApiAppPoolName"

# --------------------------------------------------------------------- build --

if (-not $SkipBuild) {
    Write-Step "Publishing the API ($Configuration)"

    dotnet publish $apiProject `
        --configuration $Configuration `
        --output $apiStaging `
        --nologo
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

    Write-Step 'Building the Angular front end'

    Push-Location $webProject
    try {
        # Run npm through cmd with its stderr folded into stdout. npm writes its
        # warnings - the allow-scripts notice, audit notes - to stderr, and with
        # $ErrorActionPreference set to Stop PowerShell turns any native command's
        # stderr into a terminating error. The build would abort on a warning while
        # npm itself had succeeded. The exit code is what actually says whether it
        # worked, and cmd passes it through.

        # ci gives a reproducible install from the lock file.
        cmd /c "npm ci 2>&1"
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }

        cmd /c "npm run build -- --configuration production --output-path ""$webStaging"" 2>&1"
        if ($LASTEXITCODE -ne 0) { throw 'ng build failed.' }
    }
    finally {
        Pop-Location
    }

    # Angular 17+ emits the browser bundle into a browser/ subfolder.
    $browserDir = Join-Path $webStaging 'browser'
    if (Test-Path $browserDir) { $webStaging = $browserDir }
}
else {
    Write-Step 'Skipping build (-SkipBuild)'
}

# --------------------------------------------------------------- preservation --

Write-Step 'Preserving environment state'

$preserved = @{}

$productionSettings = Join-Path $apiTarget 'appsettings.Production.json'
if (Test-Path $productionSettings) {
    $preserved['settings'] = Join-Path $stagingRoot 'appsettings.Production.json'
    Copy-Item $productionSettings $preserved['settings'] -Force
    Write-Host '  appsettings.Production.json preserved'
}
else {
    Write-Warning '  appsettings.Production.json not found. Create it from the template before first start.'
}

$uploads = Join-Path $apiTarget 'wwwroot\uploads'
if (Test-Path $uploads) {
    Write-Host "  uploads folder retained in place ($(@(Get-ChildItem $uploads -Recurse -File).Count) files)"
}

# ------------------------------------------------------------------- deploy --

if ($PSCmdlet.ShouldProcess($SiteRoot, 'Deploy the LEAN portal')) {

    Write-Step 'Stopping application pools'
    foreach ($pool in @($AppPoolName, $ApiAppPoolName | Select-Object -Unique)) {
        if ((Get-WebAppPoolState -Name $pool).Value -ne 'Stopped') {
            Stop-WebAppPool -Name $pool
            Write-Host "  $pool stopping..."
        }
    }

    # Give IIS a moment to release the assemblies before overwriting them.
    $deadline = (Get-Date).AddSeconds(30)
    do {
        Start-Sleep -Seconds 2
        $running = @($AppPoolName, $ApiAppPoolName | Select-Object -Unique |
            Where-Object { (Get-WebAppPoolState -Name $_).Value -ne 'Stopped' })
    } while ($running.Count -gt 0 -and (Get-Date) -lt $deadline)

    if ($running.Count -gt 0) {
        throw "Application pool(s) did not stop: $($running -join ', ')"
    }

    try {
        Write-Step 'Deploying the API'
        New-Item -ItemType Directory -Path $apiTarget -Force | Out-Null

        # /MIR deletes whatever is not in the source, so everything that lives only
        # on the server must be excluded: live uploads, the logs, and the data
        # protection keys - losing those makes the stored mail password unreadable.
        robocopy $apiStaging $apiTarget /MIR /NFL /NDL /NJH /NJS /NP `
            /XD (Join-Path $apiTarget 'wwwroot\uploads') logs keys | Out-Null

        # robocopy uses exit codes below 8 for success.
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed for the API (exit $LASTEXITCODE)." }

        if ($preserved.ContainsKey('settings')) {
            Copy-Item $preserved['settings'] $productionSettings -Force
            Write-Host '  appsettings.Production.json restored'
        }

        New-Item -ItemType Directory -Path (Join-Path $apiTarget 'wwwroot\uploads') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $apiTarget 'logs') -Force | Out-Null

        Write-Step 'Deploying the front end'

        # The API lives under the site root, so exclude it from the mirror.
        robocopy $webStaging $webTarget /MIR /NFL /NDL /NJH /NJS /NP /XD $apiTarget | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed for the front end (exit $LASTEXITCODE)." }

        $frontendConfig = Join-Path $repoRoot 'deploy\iis\web.frontend.config'
        $deployedConfig = Join-Path $webTarget 'web.config'
        Copy-Item $frontendConfig $deployedConfig -Force
        Write-Host '  web.config applied'

        # That file redirects HTTP to HTTPS. On a site with no HTTPS binding the
        # redirect points at a port nothing is listening on, which would take the
        # site off the air on the next deployment - so it comes back out here.
        # Enable-Https.ps1 restores it once it has proved the certificate works.
        $deployedSite = Get-Website | Where-Object { $_.PhysicalPath -eq $SiteRoot } | Select-Object -First 1
        $httpsBindings = @()
        if ($deployedSite) {
            $httpsBindings = @(Get-WebBinding -Name $deployedSite.Name -Protocol https -ErrorAction SilentlyContinue)
        }

        if ($httpsBindings.Count -eq 0) {
            $configXml = [xml](Get-Content $deployedConfig -Raw)
            $redirectRule = $configXml.SelectSingleNode("//rewrite/rules/rule[@name='HTTP to HTTPS']")
            if ($redirectRule) {
                $redirectRule.ParentNode.RemoveChild($redirectRule) | Out-Null
                $configXml.Save($deployedConfig)
                Write-Host '  redirect to HTTPS left out - the site has no HTTPS binding'
                Write-Host '    add one with deploy\scripts\Enable-Https.ps1'
            }
        }
        else {
            Write-Host '  redirect to HTTPS is in place'
        }
    }
    finally {
        Write-Step 'Starting application pools'
        foreach ($pool in @($AppPoolName, $ApiAppPoolName | Select-Object -Unique)) {
            Start-WebAppPool -Name $pool
            Write-Host "  $pool started"
        }
    }

    # --------------------------------------------------------- permissions --

    Write-Step 'Applying folder permissions'

    $identity = "IIS AppPool\$ApiAppPoolName"
    foreach ($path in @(
        (Join-Path $apiTarget 'wwwroot\uploads'),
        (Join-Path $apiTarget 'logs'),
        (Join-Path $apiTarget 'keys')
    )) {
        # The application pool identity must be able to write uploads and logs.
        icacls $path /grant "${identity}:(OI)(CI)M" /T /Q | Out-Null
        Write-Host "  write access granted on $path"
    }

    # ------------------------------------------------------------- verify --

    Write-Step 'Verifying'

    # Reporting only. A deployment that has already been applied must not be
    # called a failure because a check could not run - and the site is found by
    # its physical path, because this script is not given a site name.
    try {

        # A site bound to a host header does not answer on localhost - IIS routes by
        # the Host header, so the request lands on whatever site has no binding, or
        # nowhere. Ask IIS what this site is bound to and use that.
        $site = Get-Website | Where-Object { $_.PhysicalPath -eq $SiteRoot } | Select-Object -First 1
        $bindings = @(Get-WebBinding -Name $site.Name -ErrorAction SilentlyContinue |
            Where-Object { $_.protocol -eq 'http' })

        $hostHeaders = @($bindings | ForEach-Object { ($_.bindingInformation -split ':')[2] } |
            Where-Object { $_ })

        $candidates = @()
        foreach ($h in $hostHeaders) { $candidates += "http://$h/api/health" }
        $candidates += 'http://localhost/api/health'

        $healthy = $false
        foreach ($healthUrl in $candidates) {
            for ($attempt = 1; $attempt -le 6; $attempt++) {
                try {
                    $response = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 10
                    Write-Host "  API healthy at $healthUrl : $($response.status)" -ForegroundColor Green
                    $healthy = $true
                    break
                }
                catch {
                    Start-Sleep -Seconds 3
                }
            }
            if ($healthy) { break }
        }

        if (-not $healthy) {
            Write-Warning '  Health check did not succeed.'
            Write-Host   '  Tried: ' -NoNewline; Write-Host ($candidates -join ', ')
            Write-Host   ''
            Write-Host   '  This can simply mean the host name does not resolve on the server itself.'
            Write-Host   '  Confirm from a browser, then check, in this order:'
            Write-Host   "    1. ASPNETCORE_ENVIRONMENT=Production is set on the $ApiAppPoolName pool"
            Write-Host   "    2. $apiTarget\appsettings.Production.json has the connection string and a Jwt:Key of 32+ bytes"
            Write-Host   "    3. $apiTarget\logs for the startup error"
            Write-Host   '    4. Event Viewer, Windows Logs > Application, source "IIS AspNetCore Module V2"'
        }
    }
    catch {
        Write-Warning "  Could not run the health check: $($_.Exception.Message)"
        Write-Host   '  The deployment itself completed; check the site by hand.'
    }
}

Remove-Item $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ''
Write-Host 'Deployment complete.' -ForegroundColor Green
Write-Host 'Reminder: the first request after a deploy applies any pending EF Core migrations.'
