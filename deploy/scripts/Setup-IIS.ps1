<#
.SYNOPSIS
    Provisions the IIS site, application pools and folders for the LEAN portal.

.DESCRIPTION
    Run once on a new Windows Server before the first deployment. Creates the
    site root, two application pools (front end and API), the site itself and
    the /api sub-application, then applies the folder permissions the API needs
    to write uploads and logs.

    Both pools run with No Managed Code: the front end is static files, and the
    API runs in process under the ASP.NET Core Module, which is the default
    and means IIS is the server: the API sees the real scheme and the real
    client address without any forwarded headers.

.PARAMETER SiteName
    IIS site name, e.g. LeanPortal.

.PARAMETER SiteRoot
    Physical path for the site, e.g. C:\inetpub\LeanPortal.

.PARAMETER HostName
    Host header to bind, e.g. leannew.qci.org.in. Omit to bind all hosts on the port.

.PARAMETER Port
    HTTP port. Defaults to 80.

.PARAMETER CertificateThumbprint
    Thumbprint of a certificate already installed in LocalMachine\My. When
    supplied, Enable-Https.ps1 is run to bind it on port 443. Omit it and set
    TLS up afterwards with that script, which can also import a PFX.

.PARAMETER QueueLength
    Requests each application pool may hold waiting. Defaults to 20000 - sized,
    with ConcurrentRequestLimit, for 10,000 visitors at once. IIS's own default
    of 1000 answers a burst with 503 errors. Safe to re-run the script to apply
    it to an existing server.

.PARAMETER ConcurrentRequestLimit
    Requests IIS works on at once across the server (serverRuntime
    appConcurrentRequestLimit). Defaults to 20000; IIS's default is 5000.

.EXAMPLE
    .\Setup-IIS.ps1 -SiteName LeanPortal -SiteRoot C:\inetpub\LeanPortal -HostName leannew.qci.org.in

.NOTES
    Prerequisites on the server:
      - IIS with the Web Server role
      - .NET 10 Hosting Bundle (installs the ASP.NET Core Module v2)
      - IIS URL Rewrite module
    Run from an elevated PowerShell session.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)] [string] $SiteName,
    [Parameter(Mandatory = $true)] [string] $SiteRoot,
    [string] $HostName = '',
    [int]    $Port = 80,
    [string] $CertificateThumbprint = '',
    [int]    $QueueLength = 20000,
    [int]    $ConcurrentRequestLimit = 20000
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module WebAdministration -ErrorAction Stop

$webPool = $SiteName
$apiPool = "$SiteName-Api"
$apiRoot = Join-Path $SiteRoot 'api'

function Write-Step([string] $Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

# ---------------------------------------------------------------- pre-flight --

Write-Step 'Checking prerequisites'

$aspNetCoreModule = Get-WebGlobalModule -Name 'AspNetCoreModuleV2' -ErrorAction SilentlyContinue
if (-not $aspNetCoreModule) {
    Write-Warning 'AspNetCoreModuleV2 is not registered. Install the .NET 10 Hosting Bundle, then re-run.'
}

$rewriteModule = Get-WebGlobalModule -Name 'RewriteModule' -ErrorAction SilentlyContinue
if (-not $rewriteModule) {
    Write-Warning 'The URL Rewrite module is not installed. Angular deep links will 404 without it.'
}

# ------------------------------------------------------------------ folders --

if ($PSCmdlet.ShouldProcess($SiteRoot, 'Create the folder structure')) {
    Write-Step 'Creating folders'

    foreach ($path in @(
        $SiteRoot,
        $apiRoot,
        (Join-Path $apiRoot 'wwwroot\uploads\documents'),
        (Join-Path $apiRoot 'wwwroot\uploads\images'),
        (Join-Path $apiRoot 'wwwroot\uploads\general'),
        (Join-Path $apiRoot 'logs'),
        # Data protection keys. These encrypt the mail password set in the console;
        # without a writable folder they are regenerated on every recycle and the
        # stored password can no longer be decrypted.
        (Join-Path $apiRoot 'keys')
    )) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        Write-Host "  $path"
    }
}

# ------------------------------------------------------------- application pools --

if ($PSCmdlet.ShouldProcess('IIS', 'Create the application pools')) {
    Write-Step 'Creating application pools'

    foreach ($pool in @($webPool, $apiPool)) {
        if (-not (Test-Path "IIS:\AppPools\$pool")) {
            New-WebAppPool -Name $pool | Out-Null
            Write-Host "  created $pool"
        }
        else {
            Write-Host "  $pool already exists"
        }

        # No Managed Code: neither pool runs the .NET Framework runtime.
        Set-ItemProperty "IIS:\AppPools\$pool" managedRuntimeVersion ''
        Set-ItemProperty "IIS:\AppPools\$pool" managedPipelineMode 'Integrated'
        Set-ItemProperty "IIS:\AppPools\$pool" processModel.identityType 'ApplicationPoolIdentity'

        # A government portal should not recycle mid-day on a fixed schedule; use
        # a nightly recycle instead of the default 29-hour rolling interval.
        Set-ItemProperty "IIS:\AppPools\$pool" recycling.periodicRestart.time '00:00:00'
        Clear-ItemProperty "IIS:\AppPools\$pool" recycling.periodicRestart.schedule -ErrorAction SilentlyContinue
        New-ItemProperty "IIS:\AppPools\$pool" -Name recycling.periodicRestart.schedule `
            -Value @{value = '03:00:00' } -ErrorAction SilentlyContinue | Out-Null

        # Keep the worker process warm so the first visitor of the day is not slow.
        Set-ItemProperty "IIS:\AppPools\$pool" processModel.idleTimeout '00:00:00'
        Set-ItemProperty "IIS:\AppPools\$pool" startMode 'AlwaysRunning'

        # Room for a crowd. The queue holds requests that have arrived but not yet
        # started; at the default of 1,000 a burst of visitors gets "503 Service
        # Unavailable" from HTTP.sys long before the server is busy. Sized for
        # 10,000 people at once, each page asking for several files.
        Set-ItemProperty "IIS:\AppPools\$pool" queueLength $QueueLength

        # A pool that fails five times in five minutes is stopped by IIS - under a
        # load test or an attack that turns a slow site into an offline one. Allow
        # more before giving up.
        Set-ItemProperty "IIS:\AppPools\$pool" failure.rapidFailProtectionMaxCrashes 20
    }

    # Requests IIS will work on at once, across the server. The default of 5,000
    # is below the 10,000 the portal is sized for.
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' `
        -Filter 'system.webServer/serverRuntime' -Name appConcurrentRequestLimit -Value $ConcurrentRequestLimit
    Write-Host "  queue length $QueueLength per pool, $ConcurrentRequestLimit concurrent requests"
}

# --------------------------------------------------------------------- site --

if ($PSCmdlet.ShouldProcess($SiteName, 'Create the site and API application')) {
    Write-Step 'Creating the site'

    if (-not (Test-Path "IIS:\Sites\$SiteName")) {
        $binding = if ($HostName) { $HostName } else { '' }
        New-Website -Name $SiteName -PhysicalPath $SiteRoot -ApplicationPool $webPool `
            -Port $Port -HostHeader $binding -Force | Out-Null
        Write-Host "  created site $SiteName on port $Port"
    }
    else {
        Write-Host "  site $SiteName already exists"
        Set-ItemProperty "IIS:\Sites\$SiteName" physicalPath $SiteRoot
        Set-ItemProperty "IIS:\Sites\$SiteName" applicationPool $webPool
    }

    if ($CertificateThumbprint) {
        # TLS lives in one place, so that binding, verification and the redirect
        # cannot drift apart. No redirect yet - nothing is deployed at this point.
        Write-Step 'Handing over to Enable-Https.ps1'

        & (Join-Path $PSScriptRoot 'Enable-Https.ps1') `
            -SiteName $SiteName -HostName $HostName `
            -CertificateThumbprint $CertificateThumbprint -SkipRedirect
    }

    Write-Step 'Creating the /api application'

    if (-not (Get-WebApplication -Site $SiteName -Name 'api' -ErrorAction SilentlyContinue)) {
        New-WebApplication -Site $SiteName -Name 'api' -PhysicalPath $apiRoot `
            -ApplicationPool $apiPool -Force | Out-Null
        Write-Host '  created /api'
    }
    else {
        Write-Host '  /api already exists'
    }
}

# -------------------------------------------------------------- permissions --

if ($PSCmdlet.ShouldProcess($apiRoot, 'Apply folder permissions')) {
    Write-Step 'Applying permissions'

    $apiIdentity = "IIS AppPool\$apiPool"
    $webIdentity = "IIS AppPool\$webPool"

    # Read for both pools across the site.
    icacls $SiteRoot /grant "${webIdentity}:(OI)(CI)RX" /T /Q | Out-Null
    icacls $apiRoot  /grant "${apiIdentity}:(OI)(CI)RX" /T /Q | Out-Null

    # Modify only where the API genuinely writes.
    foreach ($writable in @(
        (Join-Path $apiRoot 'wwwroot\uploads'),
        (Join-Path $apiRoot 'logs'),
        (Join-Path $apiRoot 'keys')
    )) {
        icacls $writable /grant "${apiIdentity}:(OI)(CI)M" /T /Q | Out-Null
        Write-Host "  write access granted on $writable"
    }
}

# ------------------------------------------------------------------ summary --

Write-Host ''
Write-Host 'IIS provisioning complete.' -ForegroundColor Green
Write-Host ''
Write-Host 'Next steps:' -ForegroundColor Yellow
Write-Host "  1. Create the database and login:  database\scripts\01-create-database.sql"
Write-Host "  2. Copy deploy\config\appsettings.Production.template.json to"
Write-Host "     $apiRoot\appsettings.Production.json and fill in the connection"
Write-Host '     string, the JWT signing key and the seed administrator password.'
Write-Host "  3. Set the environment variable for the API application pool:"
Write-Host '     ASPNETCORE_ENVIRONMENT=Production'
Write-Host "  4. Deploy:  deploy\scripts\Deploy-LeanPortal.ps1 -SiteRoot $SiteRoot -AppPoolName $webPool -ApiAppPoolName $apiPool"
if (-not $CertificateThumbprint) {
    Write-Host "  5. Put it on HTTPS:  deploy\scripts\Enable-Https.ps1 -SiteName $SiteName -HostName $HostName -SiteRoot $SiteRoot -PfxPath <cert.pfx> -PfxPassword (Read-Host -AsSecureString)"
}
