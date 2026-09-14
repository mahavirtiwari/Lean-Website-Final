<#
.SYNOPSIS
    Backs up the LEAN portal: the database, and the files that live only on the server.

.DESCRIPTION
    -Type Full  the database, plus a zip of the uploads folder, the data
                protection keys and appsettings.Production.json. The keys matter:
                without them the mail and Zoho passwords saved in the CMS cannot
                be read back after a restore.
    -Type Log   a transaction log backup (FULL recovery model only). Keeps the log
                from growing without limit and allows a point-in-time restore.

    Connects with the portal's own connection string, read from the settings
    file, so no password is stored for backups. Go-Live.ps1 grants that login
    (or SYSTEM, for the application pool identity) db_backupoperator and
    schedules this script.

    Old backups are removed after -RetainDays. Everything is logged to
    <BackupRoot>\backup.log, and a failure ends with exit code 1 so the scheduled
    task's history shows it.

.EXAMPLE
    .\Backup-LeanPortal.ps1 -SiteRoot C:\inetpub\LeanPortal -DatabaseName LeanPortal -BackupRoot E:\LeanPortalBackups -Type Full

.NOTES
    Restoring - see docs\go-live.md, "Restore from a backup".
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $SiteRoot,
    [Parameter(Mandatory = $true)] [string] $DatabaseName,
    [Parameter(Mandatory = $true)] [string] $BackupRoot,
    [ValidateSet('Full', 'Log')] [string] $Type = 'Full',
    [string] $SqlBackupPath = '',
    [int] $RetainDays = 14
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$BackupRoot = $BackupRoot.TrimEnd('\')
$logFile = Join-Path $BackupRoot 'backup.log'
New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

function Write-Log([string] $Message) {
    $line = '{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}' -f (Get-Date), $Type, $Message
    Add-Content -Path $logFile -Value $line
    Write-Host "  $line"
}

try {
    $settingsPath = Join-Path $SiteRoot 'api\appsettings.Production.json'
    if (-not (Test-Path $settingsPath)) { throw "Settings file not found: $settingsPath" }
    $connectionString = (Get-Content $settingsPath -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection

    # Only the server, credentials and TLS options are carried over, read under any
    # of their usual names (Server= or Data Source=, and so on). The portal's own
    # client understands options this .NET Framework one does not, so the string
    # is not handed over whole.
    $source = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $synonyms = [ordered]@{
        'Data Source'            = 'Data Source|Server|Address|Addr|Network Address'
        'Integrated Security'    = 'Integrated Security|Trusted_Connection'
        'User ID'                = 'User ID|UID|User'
        'Password'               = 'Password|PWD'
        'Encrypt'                = 'Encrypt'
        'TrustServerCertificate' = 'TrustServerCertificate|Trust Server Certificate'
    }
    foreach ($keyword in $synonyms.Keys) {
        $names = ($synonyms[$keyword] -split '\|' | ForEach-Object { $_ -replace ' ', '\s*' }) -join '|'
        $match = [regex]::Match($connectionString, "(?i)(?:^|;)\s*(?:$names)\s*=\s*([^;]*)")
        if ($match.Success) {
            $value = $match.Groups[1].Value.Trim()
            # Microsoft.Data.SqlClient also accepts Encrypt=Mandatory/Strict/Optional.
            if ($keyword -eq 'Encrypt') { $value = $value -notmatch '^(false|no|optional)$' }
            $source[$keyword] = $value
        }
    }
    if (-not $source.DataSource) { throw 'The connection string in the settings file names no server.' }
    $source['Initial Catalog'] = 'master'
    $source['Application Name'] = 'LEAN portal backup'

    $connection = New-Object System.Data.SqlClient.SqlConnection $source.ConnectionString
    $connection.Open()

    function Invoke-Query([string] $Sql, [hashtable] $Parameters = @{}, [switch] $Scalar) {
        $command = $connection.CreateCommand()
        $command.CommandText = $Sql
        $command.CommandTimeout = 0
        foreach ($name in $Parameters.Keys) { [void] $command.Parameters.AddWithValue("@$name", $Parameters[$name]) }
        if ($Scalar) { return $command.ExecuteScalar() }
        [void] $command.ExecuteNonQuery()
    }

    $stamp = '{0:yyyyMMdd-HHmmss}' -f (Get-Date)
    $dbDir = if ($SqlBackupPath) { $SqlBackupPath.TrimEnd('\') } else { Join-Path $BackupRoot 'database' }
    if (-not $SqlBackupPath) { New-Item -ItemType Directory -Path $dbDir -Force | Out-Null }

    # --------------------------------------------------------------- database --
    if ($Type -eq 'Log') {
        $model = Invoke-Query 'SELECT recovery_model_desc FROM sys.databases WHERE name = @db' @{ db = $DatabaseName } -Scalar
        if ($model -ne 'FULL') { Write-Log "Recovery model is $model - no log backup needed"; exit 0 }
    }

    # Compression is not available in SQL Server Express.
    $edition = [int] (Invoke-Query "SELECT CAST(SERVERPROPERTY('EngineEdition') AS int)" -Scalar)
    $compression = if ($edition -eq 4) { '' } else { ', COMPRESSION' }

    $file = Join-Path $dbDir ("{0}_{1}_{2}.{3}" -f $DatabaseName, $Type.ToLowerInvariant(), $stamp, $(if ($Type -eq 'Full') { 'bak' } else { 'trn' }))
    $verb = if ($Type -eq 'Full') { 'DATABASE' } else { 'LOG' }
    try {
        Invoke-Query @"
-- String literals are built with REPLACE, not QUOTENAME: QUOTENAME returns NULL
-- for anything over 128 characters, a long path among them, and EXEC (NULL)
-- runs nothing and reports nothing.
IF DB_ID(@db) IS NULL THROW 50000, N'The database does not exist.', 1;
DECLARE @sql nvarchar(max) = N'BACKUP $verb ' + QUOTENAME(@db) + N' TO DISK = ' + N'''' + REPLACE(@file, N'''', N'''''') + N''''
    + N' WITH INIT, CHECKSUM$compression, NAME = ' + N'''' + REPLACE(@name, N'''', N'''''') + N'''';
IF @sql IS NULL THROW 50000, N'Could not build the BACKUP statement.', 1;
EXEC (@sql);
"@ @{ db = $DatabaseName; file = $file; name = "LEAN portal $Type $stamp" }
    }
    catch {
        # 4214: no full backup yet, so there is no log chain to back up. Asked of SQL
        # Server directly: the backup history in msdb may not be readable by a
        # backup-only login.
        $sqlError = $_.Exception.GetBaseException()
        if ($Type -eq 'Log' -and $sqlError -is [System.Data.SqlClient.SqlException] -and $sqlError.Number -eq 4214) {
            Write-Log 'No full backup yet - skipping the log backup until there is one'
            exit 0
        }
        throw
    }

    # RESTORE VERIFYONLY proves the file reads back with intact checksums. It needs
    # CREATE DATABASE permission, which a backup-only login rightly lacks; then the
    # CHECKSUM taken while writing is the check.
    $verified = 'verified'
    try {
        Invoke-Query "DECLARE @sql nvarchar(max) = N'RESTORE VERIFYONLY FROM DISK = ' + N'''' + REPLACE(@file, N'''', N'''''') + N'''' + N' WITH CHECKSUM'; EXEC (@sql);" @{ file = $file }
    }
    catch { $verified = 'written with checksums (read-back check needs CREATE DATABASE permission)' }

    $size = if (Test-Path $file) { '{0:N1} MB' -f ((Get-Item $file).Length / 1MB) } else { 'on the SQL Server' }
    Write-Log "Database backed up, $verified`: $file ($size)"

    # ------------------------------------------------------------------ files --
    if ($Type -eq 'Full') {
        Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
        $filesDir = Join-Path $BackupRoot 'files'
        New-Item -ItemType Directory -Path $filesDir -Force | Out-Null

        $staging = Join-Path $env:TEMP "lean-backup-$stamp"
        New-Item -ItemType Directory -Path $staging -Force | Out-Null
        try {
            $api = Join-Path $SiteRoot 'api'
            foreach ($item in @(
                    @{ From = (Join-Path $api 'wwwroot\uploads'); To = 'uploads' },
                    @{ From = (Join-Path $api 'keys'); To = 'keys' })) {
                if (Test-Path $item.From) {
                    robocopy $item.From (Join-Path $staging $item.To) /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP | Out-Null
                    if ($LASTEXITCODE -ge 8) { throw "Copying $($item.From) failed (robocopy $LASTEXITCODE)" }
                }
            }
            Copy-Item $settingsPath (Join-Path $staging 'appsettings.Production.json')

            # Entry by entry, with forward slashes: CreateFromDirectory in Windows
            # PowerShell's .NET writes backslashes into entry names, which other
            # tools unpack as flat file names - not what a restore needs.
            $zip = Join-Path $filesDir "LeanPortal_files_$stamp.zip"
            $root = (Resolve-Path $staging).Path.TrimEnd('\') + '\'
            $archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
            try {
                foreach ($f in Get-ChildItem $staging -Recurse -File) {
                    $entry = $f.FullName.Substring($root.Length).Replace('\', '/')
                    [void] [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $f.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal)
                }
            }
            finally { $archive.Dispose() }
            Write-Log ("Files backed up: {0} ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB))
        }
        finally {
            Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # -------------------------------------------------------------- retention --
    $cutoff = (Get-Date).AddDays(-$RetainDays)
    $old = @()
    if (-not $SqlBackupPath -or (Test-Path $dbDir)) {
        $old += @(Get-ChildItem $dbDir -File -Include *.bak, *.trn -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt $cutoff })
    }
    $old += @(Get-ChildItem (Join-Path $BackupRoot 'files') -File -Filter *.zip -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt $cutoff })
    if ($old.Count -gt 0) {
        $old | Remove-Item -Force
        Write-Log "Removed $($old.Count) backup file(s) older than $RetainDays days"
    }

    $connection.Dispose()
    exit 0
}
catch {
    Write-Log "FAILED: $($_.Exception.Message)"
    exit 1
}
