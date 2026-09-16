<#
.SYNOPSIS
    Puts the LEAN portal on HTTPS: installs the certificate, binds port 443,
    proves the handshake works, and only then turns on the redirect.

.DESCRIPTION
    Ordering matters here. Redirecting HTTP to HTTPS before 443 answers takes the
    site off the air, so this script does the redirect last and only after it has
    opened a real TLS connection to the server and seen the right certificate
    come back. Nothing is redirected if that check does not pass.

    Safe to run again. Re-run it after renewing the certificate: give it the new
    thumbprint or PFX and it rebinds without touching anything else.

.PARAMETER SiteName
    IIS site name, e.g. LeanPortal.

.PARAMETER HostName
    The host the certificate is for, e.g. leannew.qci.org.in. The binding uses SNI, so
    other sites can share port 443 on this machine.

.PARAMETER CertificateThumbprint
    Thumbprint of a certificate already in LocalMachine\My. Spaces are ignored,
    so the value copied out of the certificate dialog works as-is.

.PARAMETER PfxPath
    A .pfx to import into LocalMachine\My instead. Use with -PfxPassword.

.PARAMETER PfxPassword
    The PFX password, as a SecureString:  -PfxPassword (Read-Host -AsSecureString)
    Passing it this way keeps it out of the console history and the transcript.

.PARAMETER ChainPath
    The issuer's intermediate certificates - a .crt, .cer or .p7b bundle, as
    certificate authorities supply them. Imported into LocalMachine\CA so IIS
    sends the whole chain; without it a browser on a fresh device may not trust
    the site even though the server does.

.PARAMETER SiteRoot
    Physical path of the site, e.g. C:\inetpub\LeanPortal. Needed only to apply
    the web.config that carries the redirect. Omit before the first deployment.

.PARAMETER SkipRedirect
    Bind the certificate but leave HTTP serving the site directly. Useful for
    testing a new certificate before committing to it.

.EXAMPLE
    The QCI wildcard certificate (*.qci.org.in), with the GoDaddy intermediates:

    .\Enable-Https.ps1 -SiteName LeanPortal -HostName leannew.qci.org.in `
        -PfxPath C:\certs\qci.org.in.pfx -PfxPassword (Read-Host -AsSecureString) `
        -ChainPath C:\certs\gd_dv-r1-g2_iis_intermediates.p7b `
        -SiteRoot C:\inetpub\LeanPortal

.EXAMPLE
    .\Enable-Https.ps1 -SiteName LeanPortal -HostName leannew.qci.org.in `
        -CertificateThumbprint 1a2b3c... -SiteRoot C:\inetpub\LeanPortal

.NOTES
    Two things this script cannot do for you, because they are changes to the
    machine's and the network's security posture:
      - open TCP 443 in Windows Firewall
      - open TCP 443 inbound in the Azure network security group
    It checks both and prints the exact command for the first. Run from an
    elevated PowerShell session.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)] [string] $SiteName,
    [Parameter(Mandatory = $true)] [string] $HostName,
    [string] $CertificateThumbprint = '',
    [string] $PfxPath = '',
    [securestring] $PfxPassword,
    [string] $ChainPath = '',
    [string] $SiteRoot = '',
    [switch] $SkipRedirect
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module WebAdministration -ErrorAction Stop

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

function Write-Step([string] $Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    throw "There is no IIS site called $SiteName. Run Setup-IIS.ps1 first."
}

# ------------------------------------------------------------ certificate --

Write-Step 'Finding the certificate'

$store = 'Cert:\LocalMachine\My'
$certificate = $null

if ($PfxPath) {
    if (-not (Test-Path $PfxPath)) { throw "No PFX at $PfxPath." }
    if (-not $PfxPassword) { throw 'Give -PfxPassword when importing a PFX.' }

    if ($PSCmdlet.ShouldProcess($PfxPath, 'Import into LocalMachine\My')) {
        # Not exportable. The private key never needs to leave this machine, and a
        # key that cannot be copied off it is one less thing to lose control of.
        $certificate = Import-PfxCertificate -FilePath $PfxPath `
            -CertStoreLocation $store -Password $PfxPassword
        Write-Host "  imported $($certificate.Thumbprint)"
    }
}
elseif ($CertificateThumbprint) {
    $thumbprint = ($CertificateThumbprint -replace '[^0-9A-Fa-f]', '').ToUpperInvariant()
    $certificate = Get-ChildItem $store | Where-Object { $_.Thumbprint -eq $thumbprint }
    if (-not $certificate) {
        throw "No certificate with thumbprint $thumbprint in LocalMachine\My."
    }
    Write-Host "  found $($certificate.Subject)"
}
else {
    # Nothing named, so look for one that fits. Being explicit about which
    # certificate is in use matters more than saving a parameter, so this stops
    # rather than guessing when the answer is not unique.
    $parentDomain = ''
    if ($HostName.Contains('.')) { $parentDomain = $HostName.Split('.', 2)[1] }

    $candidates = @(Get-ChildItem $store | Where-Object {
            $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date) -and
            @(Get-CertificateNames $_ | Where-Object {
                    $_ -eq $HostName -or ($parentDomain -and $_ -eq "*.$parentDomain")
                }).Count -gt 0
        })

    if ($candidates.Count -eq 1) {
        $certificate = $candidates[0]
        Write-Host "  found $($certificate.Subject) ($($certificate.Thumbprint))"
    }
    elseif ($candidates.Count -eq 0) {
        throw ("No certificate in LocalMachine\My covers $HostName. " +
            'Import one with -PfxPath, or name it with -CertificateThumbprint.')
    }
    else {
        Write-Host '  more than one certificate covers this host:'
        $candidates | ForEach-Object {
            Write-Host "    $($_.Thumbprint)  expires $($_.NotAfter.ToString('yyyy-MM-dd'))  $($_.Subject)"
        }
        throw 'Choose one and pass it as -CertificateThumbprint.'
    }
}

if (-not $certificate) { throw 'No certificate resolved.' }

# ----------------------------------------------------- certificate checks --

Write-Step 'Checking the certificate'

if (-not $certificate.HasPrivateKey) {
    throw 'That certificate has no private key, so IIS cannot serve TLS with it.'
}

$names = @(Get-CertificateNames $certificate)
$covered = $names | Where-Object {
    $_ -eq $HostName -or ($_.StartsWith('*.') -and $HostName.EndsWith($_.Substring(1)))
}
if ($covered) {
    Write-Host "  covers $HostName"
}
else {
    Write-Warning "  This certificate does not name $HostName. It covers: $($names -join ', ')"
    Write-Warning '  Browsers will refuse it. Continuing so you can see the rest of the checks.'
}

$daysLeft = [int]($certificate.NotAfter - (Get-Date)).TotalDays
if ($daysLeft -lt 0) {
    Write-Warning "  Expired on $($certificate.NotAfter.ToString('yyyy-MM-dd'))."
}
elseif ($daysLeft -lt 30) {
    Write-Warning "  Expires in $daysLeft days, on $($certificate.NotAfter.ToString('yyyy-MM-dd'))."
}
else {
    Write-Host "  valid for $daysLeft more days (to $($certificate.NotAfter.ToString('yyyy-MM-dd')))"
}

if ($ChainPath) {
    if (-not (Test-Path $ChainPath)) { throw "No intermediate bundle at $ChainPath." }

    if ($PSCmdlet.ShouldProcess($ChainPath, 'Import the intermediates into LocalMachine\CA')) {
        # Intermediates, not roots: into the intermediate store, which is where IIS
        # looks for the rest of the chain to send. A root in the bundle is left
        # to the operating system's own trust list.
        $imported = @(Import-Certificate -FilePath $ChainPath -CertStoreLocation 'Cert:\LocalMachine\CA')
        foreach ($c in $imported) { Write-Host "  intermediate: $($c.Subject)" }
    }
}

# A PFX often carries the leaf alone. The chain then builds on this machine, where
# the issuer may already be trusted, but not in a browser on a fresh device.
$chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
$chain.ChainPolicy.RevocationMode = 'NoCheck'
if ($chain.Build($certificate)) {
    Write-Host "  chain builds, $($chain.ChainElements.Count) certificates deep"
}
else {
    Write-Warning '  The chain does not build on this machine:'
    foreach ($element in $chain.ChainElements) {
        foreach ($status in $element.ChainElementStatus) {
            Write-Warning "    $($status.StatusInformation.Trim())"
        }
    }
    Write-Warning '  Usually a missing intermediate. Import the issuer bundle into'
    Write-Warning '  LocalMachine\CA, then run this again.'
}

# ---------------------------------------------------------------- binding --

Write-Step 'Binding port 443'

# Say so rather than leaving a puzzle later.
$conflicting = @()
foreach ($other in Get-Website) {
    if ($other.Name -eq $SiteName) { continue }
    foreach ($otherBinding in @($other.bindings.Collection)) {
        if ($otherBinding.protocol -ne 'https') { continue }
        $parts = $otherBinding.bindingInformation -split ':'
        if ($parts[1] -eq '443' -and $parts[2] -eq '') { $conflicting += $other.Name }
    }
}
if ($conflicting.Count -gt 0) {
    Write-Warning "  $($conflicting -join ', ') holds port 443 for every host name."
    Write-Warning '  A binding without SNI takes the whole port, so this one may never be reached.'
}

$existing = @(Get-WebBinding -Name $SiteName -Protocol https -ErrorAction SilentlyContinue |
        Where-Object { ($_.bindingInformation -split ':')[2] -eq $HostName })

if ($existing.Count -eq 0) {
    if ($PSCmdlet.ShouldProcess("$SiteName https://${HostName}:443", 'Add the binding')) {
        # SslFlags 1 is SNI: the certificate is chosen by the host name the client
        # asks for, so this does not claim the port for everything on the machine.
        New-WebBinding -Name $SiteName -Protocol https -Port 443 -HostHeader $HostName -SslFlags 1
        Write-Host "  added https://${HostName}:443"
    }
}
else {
    Write-Host "  https://${HostName}:443 already bound"
}

if ($PSCmdlet.ShouldProcess($certificate.Thumbprint, 'Attach to the binding')) {
    $binding = Get-WebBinding -Name $SiteName -Protocol https |
        Where-Object { ($_.bindingInformation -split ':')[2] -eq $HostName } |
        Select-Object -First 1

    if (-not $binding) {
        throw "The binding for ${HostName}:443 is not there to attach a certificate to."
    }

    $binding.AddSslCertificate($certificate.Thumbprint, 'My')
    Write-Host "  certificate $($certificate.Thumbprint) attached"
}

# ----------------------------------------------------------------- verify --

Write-Step 'Verifying the handshake'

# Straight to the local listener, asking for the host by name so SNI picks the
# same certificate a visitor would get. This proves TLS end to end without
# depending on DNS, on the firewall, or on the network security group.
$negotiated = $null
$presentedThumbprint = $null
$client = $null
$ssl = $null
try {
    $client = New-Object System.Net.Sockets.TcpClient
    $client.Connect('127.0.0.1', 443)

    # Any certificate is accepted here on purpose: the point is to see which one
    # comes back, not to decide whether to trust it.
    $accept = [System.Net.Security.RemoteCertificateValidationCallback] { param($a, $b, $c, $d) $true }
    $ssl = New-Object System.Net.Security.SslStream($client.GetStream(), $false, $accept)
    $ssl.AuthenticateAsClient($HostName)

    $presented = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($ssl.RemoteCertificate)
    $presentedThumbprint = $presented.Thumbprint
    $negotiated = $ssl.SslProtocol

    Write-Host "  handshake succeeded over $negotiated"
    Write-Host "  server presented $($presented.Subject)"
}
catch {
    Write-Warning "  Could not complete a TLS handshake on 443: $($_.Exception.Message)"
}
finally {
    if ($ssl) { $ssl.Dispose() }
    if ($client) { $client.Dispose() }
}

$handshakeGood = ($presentedThumbprint -eq $certificate.Thumbprint)
if ($presentedThumbprint -and -not $handshakeGood) {
    Write-Warning "  The server presented a different certificate ($presentedThumbprint)."
    Write-Warning '  Another binding is answering first. Resolve that before redirecting.'
}

# Old protocol versions are a finding in any government security review.
# Reported only - switching them off is a change to the machine, and yours to
# make. Absent registry keys are not the same as enabled: Windows Server 2022
# already refuses TLS 1.0 and 1.1 by default. What an auditor will want is for it
# to be stated rather than inherited, so the distinction is spelled out.
$schannel = 'HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols'
$notPinned = @()
foreach ($old in @('TLS 1.0', 'TLS 1.1', 'SSL 3.0')) {
    $key = Join-Path $schannel "$old\Server"
    if (Test-Path $key) {
        $enabled = (Get-ItemProperty $key -Name Enabled -ErrorAction SilentlyContinue).Enabled
        if ($enabled -eq 0) { continue }
        Write-Warning "  $old is switched ON in SCHANNEL. Turn it off before any security review."
        continue
    }
    $notPinned += $old
}
if ($notPinned.Count -gt 0) {
    Write-Host "  $($notPinned -join ', '): left at the operating system default, not disabled explicitly."
    Write-Host '    Recent Windows Server refuses them anyway. Set them to 0 under'
    Write-Host "    $schannel to be able to show it."
}

# --------------------------------------------------------------- redirect --

if ($SkipRedirect) {
    Write-Step 'Leaving HTTP as it is (-SkipRedirect)'
}
elseif (-not $handshakeGood) {
    Write-Step 'Not turning on the redirect'
    Write-Host '  The handshake check did not pass, and redirecting now would take the site'
    Write-Host '  off the air. Fix the above, then run this script again.'
}
elseif (-not $SiteRoot) {
    Write-Step 'Not turning on the redirect'
    Write-Host '  Give -SiteRoot to apply it, or run Deploy-LeanPortal.ps1, which applies it'
    Write-Host '  on its own now that the site has an HTTPS binding.'
}
else {
    Write-Step 'Turning on the redirect to HTTPS'

    $source = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'deploy\iis\web.frontend.config'
    $target = Join-Path $SiteRoot 'web.config'

    if (-not (Test-Path $source)) {
        Write-Warning "  Could not find $source. Run this from the repository."
    }
    elseif ($PSCmdlet.ShouldProcess($target, 'Apply the web.config with the redirect')) {
        Copy-Item $source $target -Force
        Write-Host '  web.config applied; HTTP now redirects to HTTPS'
    }
}

# ----------------------------------------------------------- reachability --

Write-Step 'Checking that 443 is reachable from outside'

function Test-CoversPort([object] $LocalPort, [int] $Port) {
    # Only a rule that names the port counts. A rule scoped to a program on "Any"
    # port would let 443 through as well, but matching those turns almost every
    # machine into a pass and the check into decoration. An unnecessary warning
    # costs a command nobody needed to run; a false all-clear costs an outage.
    foreach ($entry in @($LocalPort)) {
        $text = [string]$entry
        if ($text -eq [string]$Port) { return $true }
        if ($text -match '^(\d+)-(\d+)$') {
            if ([int]$Matches[1] -le $Port -and $Port -le [int]$Matches[2]) { return $true }
        }
    }
    return $false
}

$rulesFor443 = @()
$firewallReadable = $true
try {
    # Each rule is asked for its ports in turn. Querying the port filters in bulk
    # is much quicker but is refused without elevation, so this way answers the
    # same in either session. It takes about a minute on a server with the usual
    # couple of hundred rules, which is fine for something run once a year.
    Write-Host '  reading the firewall rules; this takes a moment'

    $rulesFor443 = @(Get-NetFirewallRule -Direction Inbound -Enabled True -Action Allow -ErrorAction Stop |
            Where-Object {
                $filter = $_ | Get-NetFirewallPortFilter -ErrorAction SilentlyContinue
                $filter -and $filter.Protocol -eq 'TCP' -and (Test-CoversPort $filter.LocalPort 443)
            })
}
catch {
    $firewallReadable = $false
    Write-Host "  Could not read the firewall rules: $($_.Exception.Message)"
}

if (-not $firewallReadable) {
    # Nothing to say; the reason is already printed.
}
elseif ($rulesFor443.Count -gt 0) {
    Write-Host '  Windows Firewall allows inbound TCP 443:'
    $rulesFor443 | Select-Object -First 5 | ForEach-Object {
        Write-Host "    $($_.DisplayName)"
    }
}
else {
    Write-Warning '  No inbound rule names TCP 443 in Windows Firewall. Run, from an elevated session:'
    Write-Host '    New-NetFirewallRule -DisplayName "HTTPS (443)" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow'
}

Write-Host ''
Write-Host '  The Azure network security group on this VM also has to allow inbound TCP 443.'
Write-Host '  It cannot be read from inside the machine; check it in the portal.'

# ---------------------------------------------------------------- summary --

Write-Host ''
if ($handshakeGood) {
    Write-Host 'HTTPS is serving on this machine.' -ForegroundColor Green
}
else {
    Write-Host 'HTTPS is not serving yet. See the warnings above.' -ForegroundColor Yellow
}

Write-Host ''
Write-Host 'Then check, from a machine that is not this one:' -ForegroundColor Yellow
Write-Host "  1. https://$HostName/ loads with no certificate warning"
Write-Host "  2. http://$HostName/ redirects to it"
Write-Host "  3. https://$HostName/api/site/sitemap.xml lists https:// addresses, not http://"
Write-Host '     - that is the API reading the scheme through IIS. If it says http,'
Write-Host '       search engines will be given the wrong addresses.'
Write-Host ''
Write-Host 'Renewal:' -ForegroundColor Yellow
Write-Host "  This certificate expires on $($certificate.NotAfter.ToString('dd MMM yyyy'))."
Write-Host '  Run this script again with the new PFX or thumbprint. Nothing else changes.'
Write-Host '  Put a reminder in the calendar for a month before that date: once HSTS is'
Write-Host '  raised to a year, an expired certificate leaves visitors no way through.'
