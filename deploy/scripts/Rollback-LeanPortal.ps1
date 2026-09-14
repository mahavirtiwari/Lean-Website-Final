<#
.SYNOPSIS
    Puts the previous release of the LEAN portal back.

.DESCRIPTION
    Deploy-LeanPortal.ps1 keeps the release it replaces under <SiteRoot>.releases.
    This copies one of those back - the most recent by default - with the same
    care as a deployment: uploads, logs, keys and appsettings.Production.json are
    never touched.

    It restores the code, not the database. If the release being rolled back
    changed the database schema, the older code may not work against it: restore
    the database backup taken just before that deployment as well (see
    docs\go-live.md, "Restore from a backup").

.PARAMETER SiteRoot
    e.g. C:\inetpub\LeanPortal

.PARAMETER Release
    The folder name under <SiteRoot>.releases to restore. Lists them when omitted
    and restores the newest after confirmation.

.EXAMPLE
    .\Rollback-LeanPortal.ps1 -SiteRoot C:\inetpub\LeanPortal -AppPoolName LeanPortal -ApiAppPoolName LeanPortal-Api
#>

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)] [string] $SiteRoot,
    [Parameter(Mandatory = $true)] [string] $AppPoolName,
    [string] $ApiAppPoolName,
    [string] $Release = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module WebAdministration -ErrorAction Stop

if (-not $ApiAppPoolName) { $ApiAppPoolName = $AppPoolName }
$SiteRoot = $SiteRoot.TrimEnd('\')
$apiTarget = Join-Path $SiteRoot 'api'
$releases = "$SiteRoot.releases"
$pools = @($AppPoolName, $ApiAppPoolName | Select-Object -Unique)

$available = @(Get-ChildItem $releases -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending)
if ($available.Count -eq 0) { throw "No earlier releases under $releases." }

Write-Host 'Releases kept (newest first):'
$available | ForEach-Object { Write-Host "  $($_.Name)" }

$chosen = if ($Release) { $available | Where-Object Name -eq $Release } else { $available[0] }
if (-not $chosen) { throw "No release called $Release." }
foreach ($part in 'api\LeanPortal.Api.dll', 'web\index.html') {
    if (-not (Test-Path (Join-Path $chosen.FullName $part))) { throw "Release $($chosen.Name) is incomplete ($part missing)." }
}

if (-not $PSCmdlet.ShouldProcess($SiteRoot, "Restore release $($chosen.Name)")) { return }

function Invoke-Mirror([string] $From, [string] $To, [string[]] $ExcludeDirs = @(), [string[]] $ExcludeFiles = @()) {
    $rcArgs = @($From, $To, '/MIR', '/R:2', '/W:2', '/NFL', '/NDL', '/NJH', '/NJS', '/NP')
    if ($ExcludeDirs.Count) { $rcArgs += '/XD'; $rcArgs += $ExcludeDirs }
    if ($ExcludeFiles.Count) { $rcArgs += '/XF'; $rcArgs += $ExcludeFiles }
    robocopy @rcArgs | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Copying $From to $To failed (robocopy exit $LASTEXITCODE)." }
}

foreach ($pool in $pools) { if ((Get-WebAppPoolState -Name $pool).Value -ne 'Stopped') { Stop-WebAppPool -Name $pool } }
$deadline = (Get-Date).AddSeconds(60)
do { Start-Sleep -Seconds 2; $running = @($pools | Where-Object { (Get-WebAppPoolState -Name $_).Value -ne 'Stopped' }) }
while ($running.Count -gt 0 -and (Get-Date) -lt $deadline)
if ($running.Count -gt 0) { throw "Application pool(s) did not stop: $($running -join ', ')." }

try {
    Invoke-Mirror (Join-Path $chosen.FullName 'api') $apiTarget `
        -ExcludeDirs 'uploads', 'logs', 'keys' `
        -ExcludeFiles 'appsettings.Production.json'
    Invoke-Mirror (Join-Path $chosen.FullName 'web') $SiteRoot -ExcludeDirs @($apiTarget)
    Write-Host "Restored release $($chosen.Name)." -ForegroundColor Green
}
finally {
    foreach ($pool in $pools) { Start-WebAppPool -Name $pool }
}

Write-Host 'The database was not changed. If the rolled-back release altered the schema, restore the'
Write-Host 'backup taken before that deployment too (docs\go-live.md, "Restore from a backup").'
