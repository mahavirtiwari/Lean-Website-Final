<#
.SYNOPSIS
    Builds the LEAN portal into a single zip that a server can deploy without
    building anything itself.

.DESCRIPTION
    For a production server with no .NET SDK, no Node.js, or no internet access
    for npm and NuGet. Run this on any Windows machine that has them, copy the
    zip to the server, and deploy it with:

        .\deploy\Go-Live.ps1 -PackagePath E:\LeanPortal-package-<date>.zip

    The zip holds api\ (the published API), web\ (the Angular site) and
    package.json (when and from what it was built). It contains no settings and
    no secrets - those stay on the server.

.PARAMETER OutputFolder
    Where to write the zip. Defaults to deploy\packages.

.EXAMPLE
    .\deploy\scripts\Build-Package.ps1
#>

[CmdletBinding()]
param(
    [string] $OutputFolder = (Join-Path (Split-Path -Parent $PSScriptRoot) 'packages')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$stamp = Get-Date -Format yyyyMMdd-HHmm
$work = Join-Path $env:TEMP "lean-portal-package-$stamp"
$api = Join-Path $work 'api'
$webOut = Join-Path $work 'web-build'
$web = Join-Path $work 'web'

foreach ($tool in 'dotnet', 'npm') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "$tool was not found on PATH." }
}

Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $work, $OutputFolder -Force | Out-Null

Write-Host '==> Publishing the API' -ForegroundColor Cyan
dotnet publish (Join-Path $repoRoot 'backend\src\LeanPortal.Api\LeanPortal.Api.csproj') --configuration Release --output $api --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# Never ship a settings file, or a developer's own uploads, that happened to be
# in the build folder. Uploads live on the server and are never deployed over.
Get-ChildItem $api -Filter 'appsettings.Production*.json' -ErrorAction SilentlyContinue | Remove-Item -Force
Remove-Item (Join-Path $api 'wwwroot\uploads') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $api 'logs'), (Join-Path $api 'keys') -Recurse -Force -ErrorAction SilentlyContinue

Write-Host '==> Building the Angular front end' -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot 'frontend\lean-portal')
try {
    cmd /c "npm ci 2>&1"
    if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }
    cmd /c "npm run build -- --configuration production --output-path ""$webOut"" 2>&1"
    if ($LASTEXITCODE -ne 0) { throw 'ng build failed.' }
}
finally { Pop-Location }

$browser = Join-Path $webOut 'browser'
Move-Item $(if (Test-Path $browser) { $browser } else { $webOut }) $web
Remove-Item $webOut -Recurse -Force -ErrorAction SilentlyContinue

foreach ($required in (Join-Path $api 'LeanPortal.Api.dll'), (Join-Path $api 'web.config'), (Join-Path $web 'index.html')) {
    if (-not (Test-Path $required)) { throw "Build incomplete: $required is missing." }
}

$source = 'unknown'
if (Get-Command git -ErrorAction SilentlyContinue) {
    $sha = & git -C $repoRoot rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and $sha) { $source = "commit $sha" }
}
if ($source -eq 'unknown') { $source = $repoRoot }

[ordered]@{ builtAt = (Get-Date).ToString('yyyy-MM-dd HH:mm'); builtOn = $env:COMPUTERNAME; source = $source } |
    ConvertTo-Json | Set-Content (Join-Path $work 'package.json') -Encoding UTF8

$zip = Join-Path $OutputFolder "LeanPortal-package-$stamp.zip"
Remove-Item $zip -Force -ErrorAction SilentlyContinue

# Entry by entry, with forward slashes. ZipFile.CreateFromDirectory in Windows
# PowerShell's .NET writes backslashes into entry names, which the zip format
# does not allow and which other tools unpack as flat file names.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = (Resolve-Path $work).Path.TrimEnd('\') + '\'
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem $work -Recurse -File) {
        $entry = $file.FullName.Substring($root.Length).Replace('\', '/')
        [void] [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal)
    }
}
finally { $archive.Dispose() }
Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ''
Write-Host ("Package: {0} ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB)) -ForegroundColor Green
Write-Host 'Copy it to the server and run:  .\deploy\Go-Live.ps1 -PackagePath <that zip>'
