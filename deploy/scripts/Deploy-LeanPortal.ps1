<#
.SYNOPSIS
    Builds (or unpacks) and deploys the MSME Competitive (LEAN) Scheme portal to IIS.

.DESCRIPTION
    Publishes the ASP.NET Core API and the Angular front end - or takes them from
    a package made by Build-Package.ps1 - stops the IIS application pools, copies
    the new release into place, and starts them again.

    What lives only on the server is never touched: the uploads folder, the logs,
    the data protection keys and appsettings.Production.json.

    The release being replaced is kept under <SiteRoot>.releases, the last three
    of them, so Rollback-LeanPortal.ps1 can put it back.

.PARAMETER SiteRoot
    Physical root of the IIS site, e.g. C:\inetpub\LeanPortal. The API goes to
    <SiteRoot>\api and the front end to <SiteRoot>.

.PARAMETER AppPoolName
    Application pool serving the site.

.PARAMETER ApiAppPoolName
    Application pool serving the /api application. Defaults to AppPoolName.

.PARAMETER HostName
    The site's host name, e.g. leannew.qci.org.in, for the health check.
    Found from the IIS bindings when omitted.

.PARAMETER PackagePath
    A package from Build-Package.ps1 - the .zip, or the folder it unpacks to.
    Omit to build from the code this script sits in.

.EXAMPLE
    .\Deploy-LeanPortal.ps1 -SiteRoot C:\inetpub\LeanPortal -AppPoolName LeanPortal -ApiAppPoolName LeanPortal-Api

.EXAMPLE
    .\Deploy-LeanPortal.ps1 -SiteRoot C:\inetpub\LeanPortal -AppPoolName LeanPortal -ApiAppPoolName LeanPortal-Api `
        -PackagePath E:\LeanPortal-package-20260914-1030.zip

.NOTES
    Run from an elevated PowerShell prompt on the web server. Go-Live.ps1 calls
    this for its Deploy step, after backing up the database.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)] [string] $SiteRoot,
    [Parameter(Mandatory = $true)] [string] $AppPoolName,
    [string] $ApiAppPoolName,
    [string] $HostName = '',
    [string] $PackagePath = '',
    [ValidateSet('Release', 'Debug')] [string] $Configuration = 'Release',
    [int] $KeepReleases = 3
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not $ApiAppPoolName) { $ApiAppPoolName = $AppPoolName }
$SiteRoot = $SiteRoot.TrimEnd('\')

$repoRoot    = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$stagingRoot = Join-Path $env:TEMP "lean-portal-publish-$(Get-Date -Format yyyyMMddHHmmss)"
$apiStaging  = Join-Path $stagingRoot 'api'
$webStaging  = Join-Path $stagingRoot 'web'
$apiTarget   = Join-Path $SiteRoot 'api'
$pools       = @($AppPoolName, $ApiAppPoolName | Select-Object -Unique)

# Server-only state, never mirrored over or deleted. By name, so a folder of the
# same name in the build (a developer's local uploads) is not copied in either.
$apiKeepDirs  = @('uploads', 'logs', 'keys')
$apiKeepFiles = @('appsettings.Production.json')

function Write-Step([string] $Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

# robocopy: exit codes below 8 mean success.
function Invoke-Mirror([string] $From, [string] $To, [string[]] $ExcludeDirs = @(), [string[]] $ExcludeFiles = @()) {
    $rcArgs = @($From, $To, '/MIR', '/R:2', '/W:2', '/NFL', '/NDL', '/NJH', '/NJS', '/NP')
    if ($ExcludeDirs.Count) { $rcArgs += '/XD'; $rcArgs += $ExcludeDirs }
    if ($ExcludeFiles.Count) { $rcArgs += '/XF'; $rcArgs += $ExcludeFiles }
    robocopy @rcArgs | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Copying $From to $To failed (robocopy exit $LASTEXITCODE)." }
}

# ---------------------------------------------------------------- pre-flight --

Write-Step 'Checking prerequisites'

if (-not (Test-Path $SiteRoot)) { throw "Site root '$SiteRoot' does not exist. Run Setup-IIS.ps1 first." }

Import-Module WebAdministration -ErrorAction Stop
foreach ($pool in $pools) {
    if (-not (Test-Path "IIS:\AppPools\$pool")) { throw "Application pool '$pool' does not exist." }
}

if (-not $PackagePath) {
    foreach ($tool in 'dotnet', 'npm') {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "$tool was not found on PATH. Install it, or build a package elsewhere with Build-Package.ps1 and pass -PackagePath."
        }
    }
}

if (-not (Test-Path (Join-Path $apiTarget 'appsettings.Production.json'))) {
    Write-Warning 'api\appsettings.Production.json does not exist yet - the API will not start without it (Go-Live.ps1 -Step Settings creates it).'
}

Write-Host "  Source     : $(if ($PackagePath) { $PackagePath } else { "$repoRoot (build)" })"
Write-Host "  Site root  : $SiteRoot"
Write-Host "  App pools  : $($pools -join ', ')"

# ------------------------------------------------------------- build / unpack --

if ($PackagePath) {
    Write-Step 'Unpacking the package'
    if (-not (Test-Path $PackagePath)) { throw "Package not found: $PackagePath" }

    if ((Get-Item $PackagePath).PSIsContainer) {
        $packageDir = $PackagePath
    }
    else {
        $packageDir = Join-Path $stagingRoot 'package'
        Expand-Archive -Path $PackagePath -DestinationPath $packageDir -Force
    }
    $apiStaging = Join-Path $packageDir 'api'
    $webStaging = Join-Path $packageDir 'web'

    $manifest = Join-Path $packageDir 'package.json'
    if (Test-Path $manifest) {
        $m = Get-Content $manifest -Raw | ConvertFrom-Json
        Write-Host "  built $($m.builtAt) from $($m.source)"
    }
}
else {
    Write-Step "Publishing the API ($Configuration)"
    dotnet publish (Join-Path $repoRoot 'backend\src\LeanPortal.Api\LeanPortal.Api.csproj') `
        --configuration $Configuration --output $apiStaging --nologo
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

    Write-Step 'Building the Angular front end'
    Push-Location (Join-Path $repoRoot 'frontend\lean-portal')
    try {
        # Through cmd, with stderr folded into stdout: npm writes warnings to
        # stderr, and PowerShell would turn those into a terminating error even
        # though npm succeeded. The exit code is what says whether it worked.
        cmd /c "npm ci 2>&1"
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }

        cmd /c "npm run build -- --configuration production --output-path ""$webStaging"" 2>&1"
        if ($LASTEXITCODE -ne 0) { throw 'ng build failed.' }
    }
    finally { Pop-Location }

    # Angular emits the browser bundle into a browser\ subfolder.
    if (Test-Path (Join-Path $webStaging 'browser')) { $webStaging = Join-Path $webStaging 'browser' }
}

# A wrong or half-built source must never be mirrored over the live site: /MIR
# would delete everything that is not in it.
foreach ($required in @((Join-Path $apiStaging 'LeanPortal.Api.dll'), (Join-Path $apiStaging 'web.config'), (Join-Path $webStaging 'index.html'))) {
    if (-not (Test-Path $required)) { throw "The release is incomplete - $required is missing. Nothing was deployed." }
}

# ------------------------------------------------------------------- deploy --

if ($PSCmdlet.ShouldProcess($SiteRoot, 'Deploy the LEAN portal')) {

    # ------------------------------------------------------- keep the old --
    $releases = "$SiteRoot.releases"
    if (Test-Path (Join-Path $apiTarget 'LeanPortal.Api.dll')) {
        Write-Step 'Keeping the current release for rollback'
        $snapshot = Join-Path $releases (Get-Date -Format yyyyMMdd-HHmmss)
        Invoke-Mirror $apiTarget (Join-Path $snapshot 'api') -ExcludeDirs $apiKeepDirs -ExcludeFiles $apiKeepFiles
        Invoke-Mirror $SiteRoot (Join-Path $snapshot 'web') -ExcludeDirs @($apiTarget)
        Write-Host "  $snapshot"

        @(Get-ChildItem $releases -Directory | Sort-Object Name -Descending | Select-Object -Skip $KeepReleases) |
            ForEach-Object { Remove-Item $_.FullName -Recurse -Force; Write-Host "  removed old release $($_.Name)" }
    }

    Write-Step 'Stopping application pools'
    foreach ($pool in $pools) {
        if ((Get-WebAppPoolState -Name $pool).Value -ne 'Stopped') {
            Stop-WebAppPool -Name $pool
            Write-Host "  $pool stopping..."
        }
    }

    # Give IIS a moment to release the assemblies before overwriting them.
    $deadline = (Get-Date).AddSeconds(60)
    do {
        Start-Sleep -Seconds 2
        $running = @($pools | Where-Object { (Get-WebAppPoolState -Name $_).Value -ne 'Stopped' })
    } while ($running.Count -gt 0 -and (Get-Date) -lt $deadline)
    if ($running.Count -gt 0) { throw "Application pool(s) did not stop: $($running -join ', '). Nothing was copied." }

    try {
        Write-Step 'Deploying the API'
        New-Item -ItemType Directory -Path $apiTarget -Force | Out-Null
        Invoke-Mirror $apiStaging $apiTarget -ExcludeDirs $apiKeepDirs -ExcludeFiles $apiKeepFiles
        foreach ($dir in 'wwwroot\uploads', 'logs', 'keys') { New-Item -ItemType Directory -Path (Join-Path $apiTarget $dir) -Force | Out-Null }
        Write-Host '  done (uploads, logs, keys and appsettings.Production.json untouched)'

        Write-Step 'Deploying the front end'
        # The API lives under the site root, so it is excluded from this mirror.
        Invoke-Mirror $webStaging $SiteRoot -ExcludeDirs @($apiTarget)

        $deployedConfig = Join-Path $SiteRoot 'web.config'
        Copy-Item (Join-Path $repoRoot 'deploy\iis\web.frontend.config') $deployedConfig -Force
        Write-Host '  web.config applied'

        # That file redirects HTTP to HTTPS. On a site with no HTTPS binding the
        # redirect points at a port nothing is listening on, which would take the
        # site off the air - so it comes back out until Enable-Https.ps1 has proved
        # the certificate works.
        $site = Get-Website | Where-Object { $_.PhysicalPath.TrimEnd('\') -eq $SiteRoot } | Select-Object -First 1
        $httpsBindings = @()
        if ($site) { $httpsBindings = @(Get-WebBinding -Name $site.Name -Protocol https -ErrorAction SilentlyContinue) }

        if ($httpsBindings.Count -eq 0) {
            $configXml = [xml](Get-Content $deployedConfig -Raw)
            $redirectRule = $configXml.SelectSingleNode("//rewrite/rules/rule[@name='HTTP to HTTPS']")
            if ($redirectRule) {
                [void] $redirectRule.ParentNode.RemoveChild($redirectRule)
                $configXml.Save($deployedConfig)
                Write-Host '  redirect to HTTPS left out for now - the site has no HTTPS binding yet'
            }
        }
        else { Write-Host '  redirect to HTTPS is in place' }
    }
    finally {
        Write-Step 'Starting application pools'
        foreach ($pool in $pools) {
            Start-WebAppPool -Name $pool
            Write-Host "  $pool started"
        }
    }

    Write-Step 'Applying folder permissions'
    $identity = "IIS AppPool\$ApiAppPoolName"
    foreach ($path in @((Join-Path $apiTarget 'wwwroot\uploads'), (Join-Path $apiTarget 'logs'), (Join-Path $apiTarget 'keys'))) {
        icacls $path /grant "${identity}:(OI)(CI)M" /T /Q | Out-Null
        Write-Host "  write access for $identity on $path"
    }

    # ----------------------------------------------------------- verify --
    Write-Step 'Health check'
    try {
        if (-not $HostName -and $site) {
            $HostName = @(Get-WebBinding -Name $site.Name | ForEach-Object { ($_.bindingInformation -split ':')[2] } | Where-Object { $_ }) | Select-Object -First 1
        }
        if (-not $HostName) { $HostName = 'localhost' }

        # Sent to this machine but asking for the real host name, so the IIS host
        # binding and the certificate match whether or not DNS points here.
        $url = "http://$HostName/api/health"
        $healthy = $false
        # Relaxed for curl: while the API starts it reports "could not connect" on
        # stderr, which Windows PowerShell would otherwise treat as a fatal error.
        $ErrorActionPreference = 'Continue'
        for ($attempt = 1; $attempt -le 20 -and -not $healthy; $attempt++) {
            $out = & curl.exe --silent --max-time 30 --ssl-no-revoke --location `
                --resolve "${HostName}:80:127.0.0.1" --resolve "${HostName}:443:127.0.0.1" $url 2>&1 | Out-String
            if ($out -match 'healthy') { $healthy = $true } else { Start-Sleep -Seconds 3 }
        }
        $ErrorActionPreference = 'Stop'
        if ($healthy) { Write-Host "  API healthy at $url" -ForegroundColor Green }
        else {
            Write-Warning "  $url did not report healthy. Check, in this order:"
            Write-Host "    1. $apiTarget\logs for the start-up error"
            Write-Host "    2. Event Viewer > Windows Logs > Application, source 'IIS AspNetCore Module V2'"
            Write-Host "    3. $apiTarget\appsettings.Production.json - connection string and Jwt:Key"
            Write-Host '    4. Rollback-LeanPortal.ps1 puts the previous release back'
        }
    }
    catch {
        Write-Warning "  Could not run the health check: $($_.Exception.Message)"
    }
}

# Only the temporary build or unpack folder; a package folder passed in is left alone.
Remove-Item $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ''
Write-Host 'Deployment complete.' -ForegroundColor Green
