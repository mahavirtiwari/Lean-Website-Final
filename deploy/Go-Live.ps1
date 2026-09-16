#Requires -RunAsAdministrator
#Requires -Version 5.1
<#
.SYNOPSIS
    Takes the MSME Competitive (LEAN) Scheme portal live on this Windows server.

.DESCRIPTION
    Runs the whole go-live in order, from an elevated PowerShell prompt in the
    folder the code was downloaded to:

      1 Preflight      checks the server, the tools, DNS, ports, SQL Server and
                       the certificate files, and changes nothing
      2 Prerequisites  installs the IIS role services the portal needs
      3 IIS            creates the site, application pools and /api application
      4 Database       creates the database and the portal's SQL login
      5 Settings       writes api\appsettings.Production.json with a generated
                       signing key and the first administrator's password
      6 Deploy         builds (or unpacks) and deploys, then confirms the first
                       start created the schema and the administrator
      7 Https          installs the certificate, binds 443, turns on the redirect
      8 Verify         checks the live site end to end
      9 Backups        schedules nightly and 15-minute backups and runs one now

    Every step can be run again safely. Use -Step to run some of them - for a
    later update, -Step Deploy is all that is needed.

    Nothing secret is written to the console, the transcript or source control.
    The script asks for: the SQL administrator (only if SqlAdminAuth = 'Sql'),
    the first CMS administrator's password (once), and the PFX password.

.PARAMETER SettingsFile
    The settings to deploy with. Defaults to golive.settings.psd1 beside this script.

.PARAMETER Step
    One or more steps to run, in their fixed order. Defaults to All.

.PARAMETER PackagePath
    A package made by deploy\scripts\Build-Package.ps1 (a .zip or its folder).
    Use it when this server cannot build - no .NET SDK, no Node.js, or no
    internet access for npm and NuGet. Omit it to build here.

.PARAMETER PfxPath
    The certificate file. Overrides PfxPath in the settings file.

.EXAMPLE
    Set-ExecutionPolicy -Scope Process Bypass -Force
    E:\Lean-Website-Final-main\deploy\Go-Live.ps1

.EXAMPLE
    # A later update, from newly downloaded code:
    .\deploy\Go-Live.ps1 -Step Deploy, Verify

.EXAMPLE
    # Check the server without changing anything:
    .\deploy\Go-Live.ps1 -Step Preflight
#>

[CmdletBinding()]
param(
    [string] $SettingsFile = (Join-Path $PSScriptRoot 'golive.settings.psd1'),

    [ValidateSet('All', 'Preflight', 'Prerequisites', 'IIS', 'Database', 'Settings', 'Deploy', 'Https', 'Verify', 'Backups')]
    [string[]] $Step = @('All'),

    [string] $PackagePath = '',

    [string] $PfxPath = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$StepOrder = @('Preflight', 'Prerequisites', 'IIS', 'Database', 'Settings', 'Deploy', 'Https', 'Verify', 'Backups')
$RepoRoot  = Split-Path -Parent $PSScriptRoot
$Scripts   = Join-Path $PSScriptRoot 'scripts'

# ============================================================== output helpers ==

function Write-Step([string] $Message) {
    Write-Host ''
    Write-Host ('=' * 78) -ForegroundColor DarkCyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host ('=' * 78) -ForegroundColor DarkCyan
}

function Write-Info([string] $Message) { Write-Host "  $Message" }
function Write-Pass([string] $Message) { Write-Host "  [ OK ] $Message" -ForegroundColor Green }
function Write-Warn([string] $Message) { Write-Host "  [WARN] $Message" -ForegroundColor Yellow }
function Write-Fail([string] $Message) { Write-Host "  [FAIL] $Message" -ForegroundColor Red }

# ==================================================================== settings ==

if (-not (Test-Path $SettingsFile)) { throw "Settings file not found: $SettingsFile" }
$S = Import-PowerShellDataFile $SettingsFile

foreach ($key in 'HostName', 'SiteName', 'SiteRoot', 'SqlServer', 'DatabaseName', 'SqlAdminAuth',
    'SqlAppAuth', 'AdminEmail', 'BackupRoot', 'RecoveryModel') {
    if (-not $S.ContainsKey($key) -or -not "$($S[$key])".Trim()) {
        throw "Settings file: '$key' must have a value."
    }
}
if ($S.SqlAdminAuth -notin 'Windows', 'Sql') { throw "SqlAdminAuth must be 'Windows' or 'Sql'." }
if ($S.SqlAppAuth -notin 'AppPoolIdentity', 'SqlLogin') { throw "SqlAppAuth must be 'AppPoolIdentity' or 'SqlLogin'." }
if ($S.RecoveryModel -notin 'FULL', 'SIMPLE') { throw "RecoveryModel must be 'FULL' or 'SIMPLE'." }

function Get-Setting([string] $Name, $Default) {
    if ($S.ContainsKey($Name) -and $null -ne $S[$Name] -and "$($S[$Name])" -ne '') { return $S[$Name] }
    return $Default
}

$SiteRoot     = $S.SiteRoot.TrimEnd('\')
$ApiRoot      = Join-Path $SiteRoot 'api'
$WebPool      = $S.SiteName
$ApiPool      = "$($S.SiteName)-Api"
$SettingsPath = Join-Path $ApiRoot 'appsettings.Production.json'
$BackupRoot   = $S.BackupRoot.TrimEnd('\')

$steps = @(if ($Step -contains 'All') { $StepOrder } else { $StepOrder | Where-Object { $Step -contains $_ } })

# A transcript of what was done, for the change record. Secrets are read with
# Read-Host -AsSecureString or Get-Credential, which transcripts do not capture.
$logDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$transcript = Join-Path $logDir ("golive-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date))
Start-Transcript -Path $transcript | Out-Null

# ======================================================================= SQL =====

$script:SqlAdminCredential = $null

function Test-LocalSqlServer {
    $name = ($S.SqlServer -split '\\')[0].Trim().ToLowerInvariant()
    return $name -in @('.', 'localhost', '(local)', '127.0.0.1', $env:COMPUTERNAME.ToLowerInvariant())
}

function Get-AdminConnectionString([string] $Database = 'master') {
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $b['Data Source'] = $S.SqlServer
    $b['Initial Catalog'] = $Database
    $b['Encrypt'] = $true
    $b['TrustServerCertificate'] = [bool](Get-Setting 'SqlTrustServerCertificate' $true)
    $b['Connect Timeout'] = 15
    $b['Application Name'] = 'LEAN portal Go-Live'

    if ($S.SqlAdminAuth -eq 'Windows') {
        $b['Integrated Security'] = $true
    }
    else {
        if (-not $script:SqlAdminCredential) {
            $script:SqlAdminCredential = Get-Credential -Message "SQL Server administrator for $($S.SqlServer) (for example sa)"
            if (-not $script:SqlAdminCredential) { throw 'No SQL administrator given.' }
        }
        $b['User ID'] = $script:SqlAdminCredential.UserName
        $b['Password'] = $script:SqlAdminCredential.GetNetworkCredential().Password
    }
    return $b.ConnectionString
}

# Runs T-SQL with named parameters. Values always travel as parameters - never
# pasted into the SQL - so names and passwords cannot change what it does.
function Invoke-Sql {
    param(
        [Parameter(Mandatory = $true)] [string] $ConnectionString,
        [Parameter(Mandatory = $true)] [string] $Query,
        [hashtable] $Parameters = @{},
        [switch] $Scalar,
        [int] $TimeoutSeconds = 300
    )
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = $TimeoutSeconds
        foreach ($name in $Parameters.Keys) {
            $value = $Parameters[$name]
            if ($null -eq $value) { $value = [DBNull]::Value }
            [void] $command.Parameters.AddWithValue("@$name", $value)
        }
        if ($Scalar) { return $command.ExecuteScalar() }

        $table = New-Object System.Data.DataTable
        $reader = $command.ExecuteReader()
        try { $table.Load($reader) } finally { $reader.Close() }
        return , $table
    }
    finally {
        $connection.Dispose()
    }
}

# ================================================================== secrets =====

function New-RandomBytesBase64([int] $Count) {
    $bytes = New-Object byte[] $Count
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [Convert]::ToBase64String($bytes)
}

# A long random password with every character class SQL Server's policy asks for.
function New-RandomPassword([int] $Length = 32) {
    $sets = @('ABCDEFGHJKLMNPQRSTUVWXYZ', 'abcdefghijkmnopqrstuvwxyz', '23456789', '!#%*+-=?@^_')
    $all = -join $sets
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $pick = {
            param($chars)
            $b = New-Object byte[] 4
            $rng.GetBytes($b)
            $chars[[BitConverter]::ToUInt32($b, 0) % $chars.Length]
        }
        $chars = New-Object System.Collections.Generic.List[char]
        foreach ($set in $sets) { $chars.Add((& $pick $set)) }
        while ($chars.Count -lt $Length) { $chars.Add((& $pick $all)) }
        # Shuffle, so the class-guaranteed characters are not always first.
        for ($i = $chars.Count - 1; $i -gt 0; $i--) {
            $b = New-Object byte[] 4
            $rng.GetBytes($b)
            $j = [BitConverter]::ToUInt32($b, 0) % ($i + 1)
            $tmp = $chars[$i]; $chars[$i] = $chars[$j]; $chars[$j] = $tmp
        }
        return -join $chars
    }
    finally { $rng.Dispose() }
}

function ConvertFrom-SecureStringPlain([securestring] $Secure) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

# The same rules the portal applies to every password.
function Test-AdminPassword([string] $Password) {
    $problems = @()
    if ($Password.Length -lt 12) { $problems += 'at least 12 characters' }
    if ($Password -cnotmatch '[A-Z]') { $problems += 'a capital letter' }
    if ($Password -cnotmatch '[a-z]') { $problems += 'a small letter' }
    if ($Password -notmatch '[0-9]') { $problems += 'a digit' }
    if ($Password -notmatch '[^A-Za-z0-9]') { $problems += 'a symbol' }
    if ($Password -eq 'ChangeMe@Lean2026') { $problems += 'not the published development password' }
    return $problems
}

# ========================================================== settings file I/O ===

function Read-ProductionSettings {
    if (-not (Test-Path $SettingsPath)) { return $null }
    return (Get-Content $SettingsPath -Raw | ConvertFrom-Json)
}

function Save-ProductionSettings($Object) {
    $json = $Object | ConvertTo-Json -Depth 20
    # ConvertTo-Json in Windows PowerShell escapes these; the file is valid either
    # way, but a person reading it should see the characters.
    # Only an escape that is not itself preceded by an escaped backslash.
    $unescaped = '(?<=(?:^|[^\\])(?:\\\\)*)\\u00'
    $json = $json -replace "${unescaped}27", "'" -replace "${unescaped}3c", '<' -replace "${unescaped}3e", '>' -replace "${unescaped}26", '&'
    [IO.File]::WriteAllText($SettingsPath, $json, (New-Object Text.UTF8Encoding $false))
    Protect-ProductionSettings
}

# Readable only by administrators, SYSTEM and the API's own application pool.
function Protect-ProductionSettings {
    if (-not (Test-Path $SettingsPath)) { return }
    icacls $SettingsPath /inheritance:r /grant:r 'BUILTIN\Administrators:(F)' 'NT AUTHORITY\SYSTEM:(F)' "IIS AppPool\${ApiPool}:(R)" /Q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not restrict access to $SettingsPath." }
}

function Protect-KeysFolder {
    $keys = Join-Path $ApiRoot 'keys'
    New-Item -ItemType Directory -Path $keys -Force | Out-Null
    icacls $keys /inheritance:r /grant:r 'BUILTIN\Administrators:(OI)(CI)F' 'NT AUTHORITY\SYSTEM:(OI)(CI)F' "IIS AppPool\${ApiPool}:(OI)(CI)M" /T /Q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not restrict access to $keys." }
}

function Set-JsonProperty($Object, [string] $Name, $Value) {
    if ($Object.PSObject.Properties[$Name]) { $Object.$Name = $Value }
    else { $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value }
}

# ============================================================== certificate =====

function Find-CertificateFile([string] $Pattern, [string] $Explicit) {
    if ($Explicit) {
        if (-not (Test-Path $Explicit)) { throw "File not found: $Explicit" }
        return (Resolve-Path $Explicit).Path
    }
    $found = @()
    foreach ($dir in @($RepoRoot, 'E:\certs', 'C:\certs')) {
        if (Test-Path $dir) {
            $found += @(Get-ChildItem -Path $dir -Filter $Pattern -File -Recurse -Depth 2 -ErrorAction SilentlyContinue)
        }
    }
    $found = @($found | Sort-Object FullName -Unique)
    if ($found.Count -eq 1) { return $found[0].FullName }
    if ($found.Count -gt 1) {
        Write-Warn "More than one $Pattern found - set it in the settings file:"
        $found | ForEach-Object { Write-Info "    $($_.FullName)" }
    }
    return ''
}

# The names a certificate covers, defensively. A certificate with no subject
# alternative names has an empty DnsNameList, and piping that into Where-Object
# sends one null through, which strict mode reports as a missing property - the
# check then fails on any unrelated certificate that happens to be in the store.
function Get-CertificateNames($Certificate) {
    $names = @()
    $list = $null
    if ($Certificate.PSObject.Properties['DnsNameList']) { $list = $Certificate.DnsNameList }
    foreach ($entry in @($list)) {
        if ($null -eq $entry) { continue }
        if ($entry -is [string]) { $names += $entry }
        elseif ($entry.PSObject.Properties['Unicode']) { $names += $entry.Unicode }
        elseif ($entry.PSObject.Properties['Punycode']) { $names += $entry.Punycode }
    }
    if ($names.Count -eq 0) {
        # No SAN list: fall back to the common name in the subject.
        $cn = $Certificate.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::DnsName, $false)
        if ($cn) { $names += $cn }
    }
    return $names
}

function Get-InstalledSiteCertificate {
    $parent = $S.HostName.Split('.', 2)[1]
    return @(Get-ChildItem Cert:\LocalMachine\My | Where-Object {
            $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date) -and
            @(Get-CertificateNames $_ | Where-Object { $_ -eq $S.HostName -or $_ -eq "*.$parent" }).Count -gt 0
        } | Sort-Object NotAfter -Descending)
}

# ================================================================ http checks ===

# curl.exe is part of Windows Server 2019 and later. --resolve sends the request
# to this machine while still asking for the real host name, so the IIS host
# header binding and the certificate's SNI both match - whether or not DNS on
# the server itself points at the server.
function Invoke-LocalRequest([string] $Url, [switch] $Body, [switch] $Follow, [int] $TimeoutSec = 60) {
    $hostName = ([Uri] $Url).Host
    $curlArgs = @('--silent', '--show-error', '--max-time', "$TimeoutSec", '--ssl-no-revoke',
        '--resolve', "${hostName}:80:127.0.0.1", '--resolve', "${hostName}:443:127.0.0.1", '--dump-header', '-')
    if ($Follow) { $curlArgs += '--location' }
    if (-not $Body) { $curlArgs += @('--output', 'NUL') }
    # curl reports "could not connect" on stderr while the site is still starting.
    # Windows PowerShell turns a native program's stderr into a terminating error
    # when ErrorActionPreference is Stop, so it is relaxed for this one call.
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $raw = & curl.exe @curlArgs $Url 2>&1 | Out-String }
    finally { $ErrorActionPreference = $previous }
    $code = 0
    $statusLines = [regex]::Matches($raw, '(?m)^HTTP/[\d.]+ (\d{3})')
    if ($statusLines.Count -gt 0) { $code = [int] $statusLines[$statusLines.Count - 1].Groups[1].Value }
    return [pscustomobject]@{ Status = $code; Raw = $raw; Curl = $LASTEXITCODE }
}

# =================================================================== steps ======

$script:Failures = 0

function Step-Preflight {
    Write-Step '1/9  Preflight - checking the server (nothing is changed)'
    $fails = 0

    # Files downloaded as a zip carry the internet zone mark, and PowerShell
    # refuses to run the scripts this one calls. Clearing it is always safe.
    $blocked = @(Get-ChildItem $PSScriptRoot -Recurse -File -Include *.ps1, *.psm1, *.psd1 -ErrorAction SilentlyContinue |
        Where-Object { Get-Item $_.FullName -Stream Zone.Identifier -ErrorAction SilentlyContinue })
    if ($blocked.Count -gt 0) {
        $blocked | Unblock-File
        Write-Pass "Unblocked $($blocked.Count) downloaded files"
    }

    $os = Get-CimInstance Win32_OperatingSystem
    if ([int] $os.BuildNumber -ge 17763) { Write-Pass "$($os.Caption) (build $($os.BuildNumber))" }
    else { Write-Fail "$($os.Caption) - Windows Server 2019 or later is required"; $fails++ }

    $drive = Split-Path -Qualifier $SiteRoot
    $disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='$drive'" -ErrorAction SilentlyContinue
    if ($disk) {
        $freeGb = [math]::Round($disk.FreeSpace / 1GB, 1)
        if ($freeGb -ge 5) { Write-Pass "$freeGb GB free on $drive" } else { Write-Fail "Only $freeGb GB free on $drive (5 GB needed)"; $fails++ }
    }
    else { Write-Fail "Drive $drive for SiteRoot does not exist"; $fails++ }

    # --------------------------------------------------------------- IIS ----
    $iis = $null
    if (Get-Command Get-WindowsFeature -ErrorAction SilentlyContinue) { $iis = Get-WindowsFeature Web-Server -ErrorAction SilentlyContinue }
    if ($iis -and $iis.Installed) { Write-Pass 'IIS is installed' }
    else { Write-Warn 'IIS is not installed yet - the Prerequisites step installs it' }

    $ancm = Test-Path "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
    if ($ancm) { Write-Pass '.NET Hosting Bundle (ASP.NET Core Module V2) is installed' }
    else {
        Write-Fail '.NET 10 Hosting Bundle is not installed'
        Write-Info '       https://dotnet.microsoft.com/download/dotnet/10.0  ->  "Hosting Bundle"'
        Write-Info '       Install it AFTER IIS (run -Step Prerequisites first), then run: iisreset'
        $fails++
    }

    $runtimes = @()
    if (Get-Command dotnet -ErrorAction SilentlyContinue) { $runtimes = @(& dotnet --list-runtimes 2>$null) }
    if (@($runtimes | Where-Object { $_ -match '^Microsoft\.AspNetCore\.App 10\.' }).Count -gt 0) {
        Write-Pass 'ASP.NET Core 10 runtime is present'
    }
    else { Write-Fail 'ASP.NET Core 10 runtime not found (it comes with the Hosting Bundle)'; $fails++ }

    if (Test-Path "$env:windir\System32\inetsrv\rewrite.dll") { Write-Pass 'IIS URL Rewrite is installed' }
    else {
        Write-Fail 'IIS URL Rewrite 2.1 is not installed'
        Write-Info '       https://www.iis.net/downloads/microsoft/url-rewrite'
        $fails++
    }

    # ------------------------------------------------------------- build ----
    if ($PackagePath) {
        if (Test-Path $PackagePath) { Write-Pass "Deploying the package $PackagePath (no build on this server)" }
        else { Write-Fail "Package not found: $PackagePath"; $fails++ }
    }
    else {
        foreach ($p in 'backend\src\LeanPortal.Api\LeanPortal.Api.csproj', 'frontend\lean-portal\package-lock.json') {
            if (-not (Test-Path (Join-Path $RepoRoot $p))) { Write-Fail "Code not found: $p (is this the downloaded folder?)"; $fails++ }
        }

        $sdks = @()
        if (Get-Command dotnet -ErrorAction SilentlyContinue) { $sdks = @(& dotnet --list-sdks 2>$null) }
        if (@($sdks | Where-Object { $_ -match '^10\.' }).Count -gt 0) { Write-Pass '.NET 10 SDK is installed (for building)' }
        else {
            Write-Fail '.NET 10 SDK is not installed - needed to build on this server'
            Write-Info '       Install it, or build a package elsewhere: deploy\scripts\Build-Package.ps1'
            $fails++
        }

        $node = if (Get-Command node -ErrorAction SilentlyContinue) { (& node --version) -replace '^v', '' } else { '' }
        if ($node) {
            $v = [version] $node
            $ok = ($v.Major -eq 22 -and $v -ge [version]'22.22.3') -or ($v.Major -eq 24 -and $v -ge [version]'24.15.0') -or $v.Major -ge 26
            if ($ok) { Write-Pass "Node.js $node" }
            else { Write-Fail "Node.js $node - Angular 22 needs 22.22.3+, 24.15.0+ or 26+ (24 LTS recommended)"; $fails++ }
        }
        else {
            Write-Fail 'Node.js is not installed - needed to build on this server (https://nodejs.org, 24 LTS)'
            $fails++
        }

        foreach ($registry in 'https://registry.npmjs.org/', 'https://api.nuget.org/v3/index.json') {
            try {
                Invoke-WebRequest -Uri $registry -Method Head -UseBasicParsing -TimeoutSec 15 | Out-Null
                Write-Pass "Can reach $registry"
            }
            catch {
                Write-Fail "Cannot reach $registry - the build downloads packages from it"
                Write-Info '       Allow it through the proxy, or use deploy\scripts\Build-Package.ps1 elsewhere'
                $fails++
            }
        }
    }

    # ----------------------------------------------------------- network ----
    try {
        $addresses = @(Resolve-DnsName $S.HostName -Type A -ErrorAction Stop | Where-Object { $_.Type -eq 'A' } | ForEach-Object IPAddress)
        $local = @(Get-NetIPAddress -AddressFamily IPv4 | ForEach-Object IPAddress)
        if (@($addresses | Where-Object { $local -contains $_ }).Count -gt 0) {
            Write-Pass "$($S.HostName) resolves to this server ($($addresses -join ', '))"
        }
        else {
            Write-Warn "$($S.HostName) resolves to $($addresses -join ', '), which is not an address on this server."
            Write-Info '       Fine if a firewall, NAT or WAF forwards that address here; otherwise ask for the DNS record to be changed.'
        }
    }
    catch { Write-Warn "$($S.HostName) does not resolve yet - ask QCI IT for the DNS A record before go-live" }

    foreach ($port in 80, 443) {
        $listeners = @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
            Where-Object { $_.OwningProcess -ne 4 })
        if ($listeners.Count -eq 0) { Write-Pass "Port $port is free for IIS" }
        else {
            $names = $listeners | ForEach-Object { (Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue).ProcessName } | Select-Object -Unique
            Write-Fail "Port $port is taken by another program: $($names -join ', ')"
            $fails++
        }
    }

    # --------------------------------------------------------------- SQL ----
    try {
        $info = Invoke-Sql (Get-AdminConnectionString) @"
SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(40)) AS Version,
       CAST(SERVERPROPERTY('Edition') AS nvarchar(100)) AS Edition,
       CAST(SERVERPROPERTY('IsIntegratedSecurityOnly') AS int) AS WindowsOnly,
       IS_SRVROLEMEMBER('sysadmin') AS IsSysadmin,
       DB_ID(@db) AS DbId
"@ -Parameters @{ db = $S.DatabaseName }
        $row = $info.Rows[0]
        Write-Pass "SQL Server $($row.Version) ($($row.Edition)) on $($S.SqlServer)"
        if ($row.IsSysadmin -ne 1) { Write-Fail 'The SQL account used is not a sysadmin - it cannot create the database'; $fails++ }
        if ($row.DbId -isnot [DBNull]) { Write-Info "       Database $($S.DatabaseName) already exists - it will be kept" }
        if ($S.SqlAppAuth -eq 'SqlLogin' -and $row.WindowsOnly -eq 1) {
            Write-Fail "SqlAppAuth is 'SqlLogin' but SQL Server accepts Windows logins only - enable mixed mode, or use AppPoolIdentity"
            $fails++
        }
    }
    catch { Write-Fail "Cannot connect to SQL Server $($S.SqlServer): $($_.Exception.Message)"; $fails++ }

    if ($S.SqlAppAuth -eq 'AppPoolIdentity' -and -not (Test-LocalSqlServer)) {
        Write-Fail "SqlAppAuth 'AppPoolIdentity' works only with SQL Server on this machine - use 'SqlLogin' for $($S.SqlServer)"
        $fails++
    }

    # ------------------------------------------------------- certificate ----
    $installed = @(Get-InstalledSiteCertificate)
    $pfx = Find-CertificateFile '*.pfx' $(if ($PfxPath) { $PfxPath } else { Get-Setting 'PfxPath' '' })
    if ($installed.Count -gt 0) {
        Write-Pass "Certificate already installed: $($installed[0].Subject), valid to $($installed[0].NotAfter.ToString('dd MMM yyyy'))"
    }
    elseif ($pfx) { Write-Pass "Certificate file: $pfx" }
    else { Write-Fail 'No certificate: put the .pfx in E:\certs, or set PfxPath in the settings file'; $fails++ }

    $chain = Find-CertificateFile '*.p7b' (Get-Setting 'ChainPath' '')
    if ($chain) { Write-Pass "Intermediate bundle: $chain" }
    else { Write-Warn 'No intermediate bundle (.p7b) found - some browsers may not trust the site without it' }

    if ($pfx -and $pfx.StartsWith($RepoRoot, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Warn 'The .pfx is inside the code folder. Move it to E:\certs so it is never copied or zipped with the code.'
    }

    Write-Host ''
    if ($fails -gt 0) {
        Write-Fail "$fails check(s) failed. Fix them and run again."
        $script:Failures += $fails
        return $false
    }
    Write-Pass 'Server is ready.'
    return $true
}

function Step-Prerequisites {
    Write-Step '2/9  Prerequisites - IIS role services'

    $features = @(
        'Web-Server', 'Web-WebServer', 'Web-Common-Http', 'Web-Default-Doc', 'Web-Static-Content',
        'Web-Http-Errors', 'Web-Health', 'Web-Http-Logging', 'Web-Performance', 'Web-Stat-Compression',
        'Web-Dyn-Compression', 'Web-Security', 'Web-Filtering', 'Web-AppInit', 'Web-Mgmt-Tools',
        'Web-Mgmt-Console', 'Web-Scripting-Tools'
    )
    $missing = @($features | Where-Object { -not (Get-WindowsFeature $_).Installed })
    if ($missing.Count -eq 0) {
        Write-Pass 'All IIS role services are installed'
    }
    else {
        Write-Info "Installing: $($missing -join ', ')"
        $result = Install-WindowsFeature -Name $missing
        if (-not $result.Success) { throw 'Installing the IIS role services failed.' }
        Write-Pass 'IIS role services installed'
        if ($result.RestartNeeded -eq 'Yes') { Write-Warn 'Windows asks for a restart. Restart, then run Go-Live.ps1 again.' }
    }

    if (-not (Test-Path "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll")) {
        Write-Warn 'Now install the .NET 10 Hosting Bundle (https://dotnet.microsoft.com/download/dotnet/10.0),'
        Write-Info '       then URL Rewrite 2.1 (https://www.iis.net/downloads/microsoft/url-rewrite),'
        Write-Info '       run iisreset, and run Go-Live.ps1 again.'
        return $false
    }

    # The Hosting Bundle registers its IIS module only if IIS was there first.
    Import-Module WebAdministration
    if (-not (Get-WebGlobalModule -Name AspNetCoreModuleV2 -ErrorAction SilentlyContinue)) {
        Write-Fail 'The Hosting Bundle was installed before IIS. Run its installer again and choose Repair, then iisreset.'
        return $false
    }
    Write-Pass 'ASP.NET Core Module V2 is registered with IIS'
    return $true
}

function Step-IIS {
    Write-Step '3/9  IIS - site, application pools and /api'

    & (Join-Path $Scripts 'Setup-IIS.ps1') -SiteName $S.SiteName -SiteRoot $SiteRoot -HostName $S.HostName -Port 80 -Quiet
    Import-Module WebAdministration

    if ([bool](Get-Setting 'StopDefaultWebSite' $true)) {
        $default = Get-Website -Name 'Default Web Site' -ErrorAction SilentlyContinue
        if ($default -and $default.State -eq 'Started') {
            $path = [Environment]::ExpandEnvironmentVariables($default.PhysicalPath)
            $files = @(Get-ChildItem $path -File -ErrorAction SilentlyContinue | ForEach-Object Name)
            $untouched = @($files | Where-Object { $_ -notin 'iisstart.htm', 'iisstart.png' }).Count -eq 0
            if ($untouched) {
                Stop-Website -Name 'Default Web Site'
                Set-ItemProperty 'IIS:\Sites\Default Web Site' serverAutoStart $false
                Write-Pass 'Stopped the unused Default Web Site'
            }
            else { Write-Warn 'Default Web Site has content of its own - left running' }
        }
    }

    if ([bool](Get-Setting 'OpenFirewall' $true)) {
        foreach ($rule in @(@{ Name = 'LEAN portal HTTP'; Port = 80 }, @{ Name = 'LEAN portal HTTPS'; Port = 443 })) {
            if (-not (Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue)) {
                New-NetFirewallRule -DisplayName $rule.Name -Direction Inbound -Protocol TCP -LocalPort $rule.Port -Action Allow -Profile Any | Out-Null
                Write-Pass "Windows Firewall: opened TCP $($rule.Port)"
            }
            else { Write-Pass "Windows Firewall: TCP $($rule.Port) already open" }
        }
        Write-Info 'A network firewall, WAF or cloud security group in front of this server must also allow 80 and 443.'
    }
    return $true
}

function Step-Database {
    Write-Step '4/9  Database - create it and the portal''s login'
    $admin = Get-AdminConnectionString
    $db = $S.DatabaseName

    if ((Invoke-Sql $admin 'SELECT DB_ID(@db)' -Parameters @{ db = $db } -Scalar) -is [DBNull]) {
        $dataPath = Get-Setting 'SqlDataPath' ''
        $logPath = Get-Setting 'SqlLogPath' ''
        Invoke-Sql $admin @"
DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@db);
IF @dataPath <> N'' AND @logPath <> N''
    SET @sql += N' ON PRIMARY (NAME = ' + QUOTENAME(@db + N'_Data') + N', FILENAME = ' + N'''' + REPLACE(@dataPath + N'\' + @db + N'_Data.mdf', N'''', N'''''') + N'''' + N', SIZE = 512MB, FILEGROWTH = 128MB)'
             +  N' LOG ON (NAME = ' + QUOTENAME(@db + N'_Log') + N', FILENAME = ' + N'''' + REPLACE(@logPath + N'\' + @db + N'_Log.ldf', N'''', N'''''') + N'''' + N', SIZE = 128MB, FILEGROWTH = 64MB)';
IF @sql IS NULL THROW 50000, N'Could not build the CREATE DATABASE statement.', 1;
EXEC (@sql);
"@ -Parameters @{ db = $db; dataPath = $dataPath.TrimEnd('\'); logPath = $logPath.TrimEnd('\') } | Out-Null
        Write-Pass "Created database $db"
    }
    else { Write-Pass "Database $db exists - kept as it is" }

    # Read Committed Snapshot keeps the public site reading while editors write.
    Invoke-Sql $admin @"
DECLARE @q sysname = QUOTENAME(@db), @sql nvarchar(max);
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @db AND is_read_committed_snapshot_on = 0)
BEGIN
    SET @sql = N'ALTER DATABASE ' + @q + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;'
             + N'ALTER DATABASE ' + @q + N' SET READ_COMMITTED_SNAPSHOT ON;'
             + N'ALTER DATABASE ' + @q + N' SET MULTI_USER;';
    EXEC (@sql);
END
SET @sql = N'ALTER DATABASE ' + @q + N' SET RECOVERY ' + CASE WHEN @recovery = N'SIMPLE' THEN N'SIMPLE' ELSE N'FULL' END + N';'
         + N'ALTER DATABASE ' + @q + N' SET AUTO_CLOSE OFF;'
         + N'ALTER DATABASE ' + @q + N' SET AUTO_SHRINK OFF;';
EXEC (@sql);
"@ -Parameters @{ db = $db; recovery = $S.RecoveryModel } | Out-Null
    Write-Pass "READ_COMMITTED_SNAPSHOT on, recovery model $($S.RecoveryModel)"

    if ($S.SqlAppAuth -eq 'AppPoolIdentity') {
        $login = "IIS APPPOOL\$ApiPool"
        Invoke-Sql $admin @"
IF SUSER_ID(@login) IS NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@login) + N' FROM WINDOWS WITH DEFAULT_DATABASE = ' + QUOTENAME(@db);
    EXEC (@sql);
END
"@ -Parameters @{ login = $login; db = $db } | Out-Null
        Write-Pass "Login $login"
    }
    else {
        $login = $S.SqlAppLogin
        $existing = Read-ProductionSettings
        $haveStoredPassword = $existing -and "$($existing.ConnectionStrings.DefaultConnection)" -match 'Password='
        $exists = (Invoke-Sql $admin 'SELECT SUSER_ID(@login)' -Parameters @{ login = $login } -Scalar) -isnot [DBNull]

        if (-not $exists -or -not $haveStoredPassword) {
            # A new random password - set on the login and carried to the Settings
            # step, which must run in the same go, or the password would be lost.
            if ($steps -notcontains 'Settings' -and -not (Test-Path $SettingsPath)) {
                throw 'The SQL login needs a password written to the settings file: run -Step Database, Settings together.'
            }
            $script:AppSqlPassword = New-RandomPassword 32
            Invoke-Sql $admin @"
DECLARE @sql nvarchar(max);
IF SUSER_ID(@login) IS NULL
    SET @sql = N'CREATE LOGIN ' + QUOTENAME(@login) + N' WITH PASSWORD = ' + N'''' + REPLACE(@pwd, N'''', N'''''') + N'''' + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = ' + QUOTENAME(@db);
ELSE
    SET @sql = N'ALTER LOGIN ' + QUOTENAME(@login) + N' WITH PASSWORD = ' + N'''' + REPLACE(@pwd, N'''', N'''''') + N'''';
IF @sql IS NULL THROW 50000, N'Could not build the login statement.', 1;
EXEC (@sql);
"@ -Parameters @{ login = $login; pwd = $script:AppSqlPassword; db = $db } | Out-Null
            Write-Pass "SQL login $login $(if ($exists) { 'given a new password' } else { 'created' }) (the password goes only into the protected settings file)"

            if ($steps -notcontains 'Settings') {
                $existing = Read-ProductionSettings
                $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder "$($existing.ConnectionStrings.DefaultConnection)"
                $b['Integrated Security'] = $false
                $b['User ID'] = $login
                $b['Password'] = $script:AppSqlPassword
                $existing.ConnectionStrings.DefaultConnection = $b.ConnectionString
                Save-ProductionSettings $existing
                $script:AppSqlPassword = $null
                Write-Pass 'Settings file updated with the new password - recycle the API pool for it to take effect'
            }
        }
        else { Write-Pass "SQL login $login exists and the settings file already holds its password" }
    }

    # The portal reads and writes data, and applies its own schema migrations at
    # start-up (db_ddladmin). Nothing server-wide.
    Invoke-Sql (Get-AdminConnectionString $db) @"
DECLARE @sql nvarchar(max);
IF USER_ID(@login) IS NULL
BEGIN
    SET @sql = N'CREATE USER ' + QUOTENAME(@login) + N' FOR LOGIN ' + QUOTENAME(@login);
    EXEC (@sql);
END
SET @sql = N'ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@login) + N';'
         + N'ALTER ROLE db_datawriter ADD MEMBER ' + QUOTENAME(@login) + N';'
         + N'ALTER ROLE db_ddladmin ADD MEMBER ' + QUOTENAME(@login) + N';';
EXEC (@sql);
"@ -Parameters @{ login = $login } | Out-Null
    Write-Pass "Database user $login with db_datareader, db_datawriter, db_ddladmin"
    return $true
}

function Step-Settings {
    Write-Step '5/9  Settings - api\appsettings.Production.json'

    if (-not (Test-Path $ApiRoot)) { throw "$ApiRoot does not exist - run the IIS step first." }

    $settings = Read-ProductionSettings
    $isNew = $null -eq $settings
    if ($isNew) {
        $template = Join-Path $PSScriptRoot 'config\appsettings.Production.template.json'
        $settings = Get-Content $template -Raw | ConvertFrom-Json
        Write-Info 'Creating it from deploy\config\appsettings.Production.template.json'
    }
    else { Write-Info 'It exists - keeping its values and filling in anything missing' }

    # A file from an older template may lack a section; add any that are missing.
    foreach ($section in 'ConnectionStrings', 'Jwt', 'Seed', 'DataProtection', 'Cors', 'ForwardedHeaders') {
        if (-not $settings.PSObject.Properties[$section]) { Set-JsonProperty $settings $section ([pscustomobject]@{}) }
    }
    foreach ($pair in @(@('ConnectionStrings', 'DefaultConnection'), @('Jwt', 'Key'), @('Seed', 'AdminEmail'), @('Seed', 'AdminPassword'), @('DataProtection', 'KeyPath'), @('Cors', 'AllowedOrigins'))) {
        if (-not $settings.($pair[0]).PSObject.Properties[$pair[1]]) { Set-JsonProperty $settings.($pair[0]) $pair[1] '' }
    }

    # --------------------------------------------------- connection string --
    $cs = "$($settings.ConnectionStrings.DefaultConnection)"
    $csIsPlaceholder = (-not $cs) -or $cs -match 'SQLSERVER01'
    $csMismatch = $false
    if (-not $csIsPlaceholder) {
        $current = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $cs
        $csMismatch = ($current.DataSource -ne $S.SqlServer) -or ($current.InitialCatalog -ne $S.DatabaseName) -or
            ($current.IntegratedSecurity -ne ($S.SqlAppAuth -eq 'AppPoolIdentity'))
        if ($csMismatch -and $S.SqlAppAuth -eq 'SqlLogin' -and -not $script:AppSqlPassword) {
            throw 'The database settings changed: run -Step Database, Settings together so the SQL login gets a password.'
        }
    }
    if ($csIsPlaceholder -or $csMismatch -or $script:AppSqlPassword) {
        $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $b['Data Source'] = $S.SqlServer
        $b['Initial Catalog'] = $S.DatabaseName
        if ($S.SqlAppAuth -eq 'AppPoolIdentity') { $b['Integrated Security'] = $true }
        else {
            if (-not $script:AppSqlPassword) { throw "Run the Database step first: it creates the SQL login's password." }
            $b['User ID'] = $S.SqlAppLogin
            $b['Password'] = $script:AppSqlPassword
        }
        $b['Encrypt'] = $true
        $b['TrustServerCertificate'] = [bool](Get-Setting 'SqlTrustServerCertificate' $true)
        $b['MultipleActiveResultSets'] = $true
        $b['Min Pool Size'] = 20
        $b['Max Pool Size'] = 400
        $b['Connect Timeout'] = 30
        $settings.ConnectionStrings.DefaultConnection = $b.ConnectionString
        Write-Pass "Connection string: $($S.SqlServer) / $($S.DatabaseName) ($($S.SqlAppAuth))"
    }
    else { Write-Pass 'Connection string kept' }

    # ----------------------------------------------------------- signing key --
    $key = "$($settings.Jwt.Key)"
    if (-not $key -or $key -like 'REPLACE-*' -or [Text.Encoding]::UTF8.GetByteCount($key) -lt 32) {
        $settings.Jwt.Key = New-RandomBytesBase64 48
        Write-Pass 'Generated a new 384-bit token signing key'
        if (-not $isNew) { Write-Warn 'Anyone signed in to the CMS will have to sign in again' }
    }
    else { Write-Pass 'Token signing key kept' }

    # ---------------------------------------------------------- administrator --
    $settings.Seed.AdminEmail = $S.AdminEmail
    $seedPassword = "$($settings.Seed.AdminPassword)"
    $adminExists = $false
    try {
        $adminExists = [int](Invoke-Sql (Get-AdminConnectionString $S.DatabaseName) "IF OBJECT_ID(N'dbo.AspNetUsers') IS NULL SELECT 0 ELSE SELECT COUNT(*) FROM dbo.AspNetUsers" -Scalar) -gt 0
    }
    catch { }

    if ($adminExists) {
        if ($seedPassword) { $settings.Seed.AdminPassword = ''; Write-Pass 'Administrator exists - removed the one-time password from the file' }
        else { Write-Pass 'Administrator exists' }
    }
    elseif (-not $seedPassword -or $seedPassword -like 'REPLACE-*') {
        Write-Host ''
        Write-Info "Choose the first password for $($S.AdminEmail)."
        Write-Info 'At least 12 characters, with a capital, a small letter, a digit and a symbol.'
        Write-Info 'It must be changed at first sign-in, and is removed from the server once used.'
        while ($true) {
            $first = ConvertFrom-SecureStringPlain (Read-Host '  Password' -AsSecureString)
            $problems = @(Test-AdminPassword $first)
            if ($problems.Count -gt 0) { Write-Warn "Needs $($problems -join ', ')."; continue }
            $second = ConvertFrom-SecureStringPlain (Read-Host '  Type it again' -AsSecureString)
            if ($first -cne $second) { Write-Warn 'The two did not match.'; continue }
            break
        }
        $settings.Seed.AdminPassword = $first
        $first = $null; $second = $null
        Write-Pass 'First administrator password set'
    }
    else { Write-Pass 'First administrator password already set' }

    # ------------------------------------------------------------ the rest --
    $settings.DataProtection.KeyPath = Join-Path $ApiRoot 'keys'
    $settings.Cors.AllowedOrigins = @("https://$($S.HostName)")
    $settings.AllowedHosts = $S.HostName
    $proxies = @(Get-Setting 'KnownProxies' @())
    Set-JsonProperty $settings.ForwardedHeaders 'KnownProxies' $proxies

    Save-ProductionSettings $settings
    $script:AppSqlPassword = $null
    Write-Pass "Saved $SettingsPath - readable only by Administrators, SYSTEM and $ApiPool"

    # The keys that encrypt the mail and Zoho passwords saved in the CMS. The site
    # folder lets every local user read by default; this one does not.
    Protect-KeysFolder
    Write-Pass 'Data protection keys folder readable only by Administrators, SYSTEM and the API'
    return $true
}

function Step-Deploy {
    Write-Step '6/9  Deploy - build and copy the portal into place'

    if (-not (Test-Path $SettingsPath)) { throw 'Run the Settings step first: the API cannot start without appsettings.Production.json.' }

    # Before an update, a fresh backup: a new release may change the schema.
    $hasTables = $false
    try { $hasTables = [int](Invoke-Sql (Get-AdminConnectionString $S.DatabaseName) 'SELECT COUNT(*) FROM sys.tables' -Scalar) -gt 0 } catch { }
    if ($hasTables) {
        Write-Info 'Backing up the database before deploying over it'
        Invoke-Backup -Type Full
    }

    $deployArgs = @{
        SiteRoot       = $SiteRoot
        AppPoolName    = $WebPool
        ApiAppPoolName = $ApiPool
        HostName       = $S.HostName
    }
    if ($PackagePath) { $deployArgs.PackagePath = $PackagePath }
    & (Join-Path $Scripts 'Deploy-LeanPortal.ps1') @deployArgs

    # ------------------------------------------------------- first start --
    Write-Info ''
    Write-Info 'Waiting for the API to start (the first start creates the schema and the administrator)...'
    $healthy = $false
    $deadline = (Get-Date).AddMinutes(3)
    while (-not $healthy -and (Get-Date) -lt $deadline) {
        # Followed, because once HTTPS is on the http:// address redirects.
        $r = Invoke-LocalRequest "http://$($S.HostName)/api/health" -Follow -TimeoutSec 30
        if ($r.Status -eq 200) { $healthy = $true } else { Start-Sleep -Seconds 4 }
    }
    if (-not $healthy) {
        Write-Fail 'The API did not answer /api/health within three minutes.'
        $log = Get-ChildItem (Join-Path $ApiRoot 'logs') -Filter '*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($log) { Write-Info "Last lines of $($log.FullName):"; Get-Content $log.FullName -Tail 25 | ForEach-Object { Write-Info "    $_" } }
        Write-Info 'Also check Event Viewer > Windows Logs > Application, source "IIS AspNetCore Module V2".'
        return $false
    }
    Write-Pass 'API is running'

    $counts = Invoke-Sql (Get-AdminConnectionString $S.DatabaseName) @"
SELECT (SELECT COUNT(*) FROM sys.tables) AS Tables,
       CASE WHEN OBJECT_ID(N'dbo.AspNetUsers') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.AspNetUsers) END AS Users,
       CASE WHEN OBJECT_ID(N'dbo.Pages') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.Pages) END AS Pages
"@
    $c = $counts.Rows[0]
    Write-Pass "Database: $($c.Tables) tables, $($c.Pages) pages, $($c.Users) CMS user(s)"

    $settings = Read-ProductionSettings
    if ([int] $c.Users -gt 0 -and "$($settings.Seed.AdminPassword)") {
        # Used once; nothing reads it again. Not left lying in a file.
        $settings.Seed.AdminPassword = ''
        Save-ProductionSettings $settings
        Write-Pass 'Removed the one-time administrator password from the settings file'
    }
    elseif ([int] $c.Users -eq 0) {
        Write-Fail 'No CMS administrator was created - see the API log in api\logs.'
        return $false
    }
    return $true
}

function Step-Https {
    Write-Step '7/9  HTTPS - certificate, port 443 and the redirect'

    $httpsArgs = @{ SiteName = $S.SiteName; HostName = $S.HostName; SiteRoot = $SiteRoot }

    $installed = @(Get-InstalledSiteCertificate)
    $pfx = Find-CertificateFile '*.pfx' $(if ($PfxPath) { $PfxPath } else { Get-Setting 'PfxPath' '' })
    if ($pfx) {
        Write-Info "Certificate file: $pfx"
        $httpsArgs.PfxPath = $pfx
        $httpsArgs.PfxPassword = Read-Host '  PFX password' -AsSecureString
    }
    elseif ($installed.Count -gt 0) {
        Write-Info "Using the installed certificate $($installed[0].Thumbprint)"
        $httpsArgs.CertificateThumbprint = $installed[0].Thumbprint
    }
    else { throw 'No certificate: put the .pfx in E:\certs or set PfxPath in the settings file.' }

    $chain = Find-CertificateFile '*.p7b' (Get-Setting 'ChainPath' '')
    if ($chain) { $httpsArgs.ChainPath = $chain }

    & (Join-Path $Scripts 'Enable-Https.ps1') @httpsArgs

    if ([bool](Get-Setting 'DisableLegacyTls' $false)) {
        $base = 'HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols'
        $changed = $false
        foreach ($protocol in 'SSL 2.0', 'SSL 3.0', 'TLS 1.0', 'TLS 1.1') {
            $path = "$base\$protocol\Server"
            $current = Get-ItemProperty $path -Name Enabled -ErrorAction SilentlyContinue
            if (-not $current -or $current.Enabled -ne 0) {
                New-Item $path -Force | Out-Null
                New-ItemProperty $path -Name Enabled -Value 0 -PropertyType DWord -Force | Out-Null
                New-ItemProperty $path -Name DisabledByDefault -Value 1 -PropertyType DWord -Force | Out-Null
                $changed = $true
            }
        }
        if ($changed) { Write-Warn 'Switched off SSL 2/3 and TLS 1.0/1.1 for the server - restart Windows for it to take effect' }
        else { Write-Pass 'SSL 2/3 and TLS 1.0/1.1 are already off' }
    }

    if ($pfx) {
        Write-Info ''
        Write-Warn "The certificate is now in the Windows store. Remove $pfx from this server"
        Write-Info '       (keep the original somewhere safe off the server): its password is in its file name.'
    }
    return $true
}

function Step-Verify {
    Write-Step '8/9  Verify - the live site, end to end'
    $h = $S.HostName
    $fails = 0

    function Check([string] $Name, [bool] $Ok, [string] $Detail = '') {
        if ($Ok) { Write-Pass $Name } else { Write-Fail "$Name $Detail"; $script:verifyFails++ }
    }
    $script:verifyFails = 0

    $r = Invoke-LocalRequest "http://$h/"
    Check 'http:// redirects to https://' ($r.Status -eq 301 -and $r.Raw -match "(?im)^Location: https://$([regex]::Escape($h))/") "(got $($r.Status))"

    $r = Invoke-LocalRequest "https://$h/" -Body
    Check 'Home page over HTTPS' ($r.Status -eq 200 -and $r.Raw -match '<app-root') "(got $($r.Status), curl $($r.Curl))"
    Check 'Certificate is trusted for the host name' ($r.Curl -eq 0) "(curl exit $($r.Curl) - see the chain / intermediate bundle)"
    foreach ($header in 'Strict-Transport-Security', 'Content-Security-Policy', 'X-Content-Type-Options', 'X-Frame-Options', 'Referrer-Policy') {
        Check "Header $header" ($r.Raw -match "(?im)^${header}:") '(missing)'
    }
    Check 'No Server or X-Powered-By header' ($r.Raw -notmatch '(?im)^(Server|X-Powered-By):') '(present)'

    $r = Invoke-LocalRequest "https://$h/about-scheme/introduction"
    Check 'Deep link served (URL Rewrite working)' ($r.Status -eq 200) "(got $($r.Status))"

    $r = Invoke-LocalRequest "https://$h/api/health" -Body
    Check 'API health' ($r.Status -eq 200 -and $r.Raw -match 'healthy') "(got $($r.Status))"

    $r = Invoke-LocalRequest "https://$h/api/site/settings"
    Check 'API reads the database' ($r.Status -eq 200) "(got $($r.Status))"

    $r = Invoke-LocalRequest "https://$h/api/site/sitemap.xml" -Body
    Check 'Sitemap lists https:// addresses' ($r.Status -eq 200 -and $r.Raw -match "<loc>https://$([regex]::Escape($h))") "(got $($r.Status))"

    $r = Invoke-LocalRequest "https://$h/robots.txt" -Body
    Check 'robots.txt points at the sitemap' ($r.Status -eq 200 -and $r.Raw -match "Sitemap: https://$([regex]::Escape($h))") "(got $($r.Status))"

    $r = Invoke-LocalRequest "https://$h/admin/login"
    Check 'CMS sign-in page' ($r.Status -eq 200) "(got $($r.Status))"

    $r = Invoke-LocalRequest "https://$h/api/swagger/index.html"
    Check 'API explorer (Swagger) is not public' ($r.Status -ne 200) '(it answered 200)'

    $r = Invoke-LocalRequest "https://$h/api/appsettings.Production.json"
    Check 'Settings file cannot be downloaded' ($r.Status -ne 200) '(it answered 200 - stop the site and investigate)'

    $cert = @(Get-InstalledSiteCertificate) | Select-Object -First 1
    if ($cert) {
        $days = [int] ($cert.NotAfter - (Get-Date)).TotalDays
        if ($days -gt 30) { Write-Pass "Certificate valid for $days more days (to $($cert.NotAfter.ToString('dd MMM yyyy')))" }
        else { Write-Warn "Certificate expires in $days days - renew it and re-run -Step Https" }
    }

    $legacy = @('TLS 1.0', 'TLS 1.1') | Where-Object {
        $p = Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\$_\Server" -Name Enabled -ErrorAction SilentlyContinue
        -not $p -or $p.Enabled -ne 0
    }
    if (@($legacy).Count -gt 0) { Write-Warn "$(@($legacy) -join ' and ') still allowed - audits expect TLS 1.2+ (set DisableLegacyTls)" }
    else { Write-Pass 'Only TLS 1.2 and later are allowed' }

    Write-Host ''
    if ($script:verifyFails -gt 0) {
        Write-Fail "$($script:verifyFails) check(s) failed."
        $script:Failures += $script:verifyFails
        return $false
    }
    Write-Pass "https://$h is live."
    Write-Info "Now confirm from a computer outside this network: https://$h"
    return $true
}

# Runs the backup script installed under BackupRoot (installing it first).
function Install-BackupScript {
    $opsDir = Join-Path $BackupRoot 'scripts'
    New-Item -ItemType Directory -Path $opsDir -Force | Out-Null
    Copy-Item (Join-Path $Scripts 'Backup-LeanPortal.ps1') $opsDir -Force
    return (Join-Path $opsDir 'Backup-LeanPortal.ps1')
}

function Get-BackupArguments([string] $Type) {
    $sqlPath = Get-Setting 'SqlBackupPath' ''
    return @{
        SiteRoot      = $SiteRoot
        DatabaseName  = $S.DatabaseName
        BackupRoot    = $BackupRoot
        SqlBackupPath = $sqlPath
        Type          = $Type
        RetainDays    = [int](Get-Setting 'BackupRetainDays' 14)
    }
}

function Initialize-BackupFolders {
    $dbDir = Get-Setting 'SqlBackupPath' ''
    foreach ($dir in @($BackupRoot, (Join-Path $BackupRoot 'files'))) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    # Backups hold the data protection keys and the settings file: administrators only.
    icacls $BackupRoot /inheritance:r /grant:r 'BUILTIN\Administrators:(OI)(CI)F' 'NT AUTHORITY\SYSTEM:(OI)(CI)F' /Q | Out-Null

    if (-not $dbDir) {
        if (-not (Test-LocalSqlServer)) { throw 'SQL Server is on another machine: set SqlBackupPath to a folder it can write to.' }
        $dbDir = Join-Path $BackupRoot 'database'
        New-Item -ItemType Directory -Path $dbDir -Force | Out-Null
        # SQL Server writes the .bak files itself, as its own service account.
        $svc = Invoke-Sql (Get-AdminConnectionString) "SELECT TOP 1 service_account FROM sys.dm_server_services WHERE servicename LIKE N'SQL Server (%'" -Scalar
        if ($svc -and $svc -isnot [DBNull]) {
            icacls $dbDir /grant "${svc}:(OI)(CI)M" /Q | Out-Null
            Write-Pass "SQL Server service ($svc) can write to $dbDir"
        }
    }
}

function Invoke-Backup([ValidateSet('Full', 'Log')] [string] $Type) {
    Initialize-BackupFolders
    $backupScript = Install-BackupScript
    $backupArgs = Get-BackupArguments $Type
    & $backupScript @backupArgs
    if ($LASTEXITCODE -ne 0) { throw "Backup ($Type) failed - see $BackupRoot\backup.log" }
}

function Step-Backups {
    Write-Step '9/9  Backups - schedule them and take the first one'

    Initialize-BackupFolders
    $backupScript = Install-BackupScript

    # The scheduled tasks run as SYSTEM. With the app pool identity there is no
    # stored SQL password to borrow, so SYSTEM gets a login that can back up this
    # one database and do nothing else. With a SQL login, the backup reuses the
    # portal's connection string, so that login is granted backup rights instead.
    $db = $S.DatabaseName
    $backupLogin = if ($S.SqlAppAuth -eq 'AppPoolIdentity') { 'NT AUTHORITY\SYSTEM' } else { $S.SqlAppLogin }
    Invoke-Sql (Get-AdminConnectionString) @"
IF SUSER_ID(@login) IS NULL AND @windows = 1
BEGIN
    DECLARE @c nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@login) + N' FROM WINDOWS';
    EXEC (@c);
END
"@ -Parameters @{ login = $backupLogin; windows = [int]($S.SqlAppAuth -eq 'AppPoolIdentity') } | Out-Null
    Invoke-Sql (Get-AdminConnectionString $db) @"
DECLARE @sql nvarchar(max) = N'';
IF USER_ID(@login) IS NULL SET @sql = N'CREATE USER ' + QUOTENAME(@login) + N' FOR LOGIN ' + QUOTENAME(@login) + N';';
SET @sql += N'ALTER ROLE db_backupoperator ADD MEMBER ' + QUOTENAME(@login) + N';';
EXEC (@sql);
"@ -Parameters @{ login = $backupLogin } | Out-Null
    Write-Pass "$backupLogin may back up $db"

    $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    $taskSettings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 3) -MultipleInstances IgnoreNew

    function New-BackupAction([string] $Type) {
        $a = Get-BackupArguments $Type
        $argLine = "-NoProfile -ExecutionPolicy Bypass -File `"$backupScript`" -SiteRoot `"$($a.SiteRoot)`" -DatabaseName `"$($a.DatabaseName)`" -BackupRoot `"$($a.BackupRoot)`" -Type $Type -RetainDays $($a.RetainDays)"
        if ($a.SqlBackupPath) { $argLine += " -SqlBackupPath `"$($a.SqlBackupPath)`"" }
        return New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $argLine
    }

    $at = [datetime]::ParseExact((Get-Setting 'BackupTime' '02:00'), 'HH:mm', $null)
    Register-ScheduledTask -TaskName 'LEAN portal - nightly backup' -TaskPath '\LEAN portal\' -Force `
        -Action (New-BackupAction 'Full') -Trigger (New-ScheduledTaskTrigger -Daily -At $at) `
        -Principal $principal -Settings $taskSettings `
        -Description 'Full database backup, uploads, data protection keys and settings.' | Out-Null
    Write-Pass "Nightly full backup at $($at.ToString('HH:mm'))"

    if ($S.RecoveryModel -eq 'FULL') {
        $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).Date.AddMinutes(5) -RepetitionInterval (New-TimeSpan -Minutes 15)
        Register-ScheduledTask -TaskName 'LEAN portal - log backup' -TaskPath '\LEAN portal\' -Force `
            -Action (New-BackupAction 'Log') -Trigger $trigger -Principal $principal -Settings $taskSettings `
            -Description 'Transaction log backup every 15 minutes (keeps the log from growing and allows point-in-time restore).' | Out-Null
        Write-Pass 'Transaction log backup every 15 minutes'
    }
    else {
        Unregister-ScheduledTask -TaskName 'LEAN portal - log backup' -Confirm:$false -ErrorAction SilentlyContinue
    }

    Write-Info 'Running the nightly task now, as SYSTEM, to prove it works...'
    $logFile = Join-Path $BackupRoot 'backup.log'
    $linesBefore = 0
    if (Test-Path $logFile) { $linesBefore = @(Get-Content $logFile).Count }

    Start-ScheduledTask -TaskPath '\LEAN portal\' -TaskName 'LEAN portal - nightly backup'

    # Watched by state alone, and by what the backup writes to its own log. This
    # used to also require LastRunTime to be later than the local clock reading
    # taken just before starting the task, and on the live server that never
    # became true even though the backup had run and finished - so the step sat
    # waiting for half an hour on work that was already done.
    $task = { (Get-ScheduledTask -TaskPath '\LEAN portal\' -TaskName 'LEAN portal - nightly backup').State }
    $startBy = (Get-Date).AddMinutes(2)
    while ((& $task) -ne 'Running' -and (Get-Date) -lt $startBy) { Start-Sleep -Seconds 2 }
    $deadline = (Get-Date).AddMinutes(30)
    while ((& $task) -eq 'Running' -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 5 }

    $result = (Get-ScheduledTaskInfo -TaskPath '\LEAN portal\' -TaskName 'LEAN portal - nightly backup').LastTaskResult
    $written = @()
    if (Test-Path $logFile) { $written = @(Get-Content $logFile | Select-Object -Skip $linesBefore) }

    if ($result -eq 0 -and @($written | Where-Object { $_ -notmatch 'FAILED' }).Count -gt 0) {
        $written | ForEach-Object { Write-Info "  $_" }
        Write-Pass "First backup taken - see $BackupRoot"
    }
    else {
        Write-Fail "The backup task ended with code $result"
        $written | ForEach-Object { Write-Info "  $_" }
        Write-Info "  Full log: $logFile"
        return $false
    }
    Write-Info 'Copy the backup folder off this server regularly: a backup on the same disk does not survive losing the disk.'
    return $true
}

# ==================================================================== run ========

try {
    Write-Host ''
    Write-Host "LEAN portal go-live - $($S.HostName)" -ForegroundColor White
    Write-Host "  Code     : $RepoRoot"
    Write-Host "  Site     : $SiteRoot  (pools $WebPool, $ApiPool)"
    Write-Host "  Database : $($S.DatabaseName) on $($S.SqlServer)  ($($S.SqlAppAuth))"
    Write-Host "  Steps    : $($steps -join ', ')"
    Write-Host "  Log      : $transcript"

    foreach ($name in $steps) {
        $ok = & "Step-$name"
        # A function's pipeline output includes anything it wrote; the last value is its result.
        $ok = @($ok)[-1]
        if ($ok -ne $true) {
            Write-Host ''
            Write-Host "Stopped at $name. Fix what is reported above, then run:" -ForegroundColor Red
            $remaining = $steps[[array]::IndexOf($steps, $name)..($steps.Count - 1)]
            Write-Host "  .\deploy\Go-Live.ps1 -Step $($remaining -join ', ')" -ForegroundColor Red
            exit 1
        }
    }

    if ($steps -contains 'Verify' -or $steps -contains 'Backups') {
        Write-Step 'Done - what is left to do by hand'
        Write-Info "1. Sign in at https://$($S.HostName)/admin as $($S.AdminEmail) and set a new password."
        Write-Info '2. Enquiry mail: set the mail server and each agency''s inbox, and send a test.'
        Write-Info '3. Helpdesk (Zoho): enter QCI''s LEAN credentials and test the connection.'
        Write-Info '4. Settings > Contact: replace the helpline placeholder 1800-XXX-XXXX.'
        Write-Info '5. Create named accounts for editors under Users; do not share the administrator.'
        Write-Info "6. From outside the network: https://$($S.HostName) loads with no warning."
        Write-Info "7. Submit https://$($S.HostName)/api/site/sitemap.xml to Google Search Console."
        Write-Info '8. Put a reminder a month before the certificate expires (27 January 2027).'
        Write-Info '9. After the first renewal has worked, raise HSTS to a year (see docs\deployment.md).'
    }
}
finally {
    Stop-Transcript | Out-Null
}
