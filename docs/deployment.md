# Deployment — Windows Server

How to stand up the MSME Competitive (LEAN) Scheme portal on Windows Server with
IIS and SQL Server, and how to deploy each subsequent release.

> **Going live?** Start with [go-live.md](go-live.md). `deploy\Go-Live.ps1` does
> every step below in order - server checks, IIS, the database, the settings
> file, the build, HTTPS, verification and backups - and asks only for the
> passwords. This document explains what each of those steps does and why.

---

## 1. Target architecture

```
                        ┌──────────────────────────────┐
   Internet  ─── 443 ──▶│  IIS 10 · Windows Server     │
                        │                              │
                        │  Site: LeanPortal            │
                        │   /            Angular SPA   │  ← static files
                        │   /api         ASP.NET Core  │  ← ASP.NET Core Module
                        │   /uploads     static files  │  ← served by the API
                        └───────────────┬──────────────┘
                                        │ TDS 1433
                        ┌───────────────▼──────────────┐
                        │  SQL Server                  │
                        │  Database: LeanPortal        │
                        └──────────────────────────────┘
```

The front end and the API share one site and one origin, so there is no CORS in
production and no cookie/domain complication. `/api` is a sub-application with
its own application pool, which means the API can be recycled without dropping
the static site.

---

## 2. Prerequisites

On the **web server**:

| Component | Notes |
|---|---|
| Windows Server 2019 or later | 2022 recommended |
| IIS with the Web Server role | Include Static Content, Default Document, HTTP Compression, Request Filtering |
| [.NET 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0) | Installs the ASP.NET Core Module v2. **Install after IIS**, or repair it afterwards. |
| [URL Rewrite 2.1](https://www.iis.net/downloads/microsoft/url-rewrite) | Required for Angular deep links |

On the **database server**: SQL Server 2019 or later.

On the **build machine** (may be the web server): .NET 10 SDK and Node.js 24 LTS (Angular 22 needs 22.22.3+, 24.15.0+ or 26+).

Confirm the hosting bundle registered correctly:

```powershell
Get-WebGlobalModule | Where-Object Name -like 'AspNetCore*'
Get-WebGlobalModule | Where-Object Name -eq 'RewriteModule'
```

---

## 3. Database

Run as a sysadmin on the SQL Server instance. Review the file paths in the
script first — they default to `D:\SQLData` and `D:\SQLLogs`.

```powershell
sqlcmd -S SQLSERVER01 -i .\database\scripts\01-create-database.sql
```

This creates the database, sets `READ_COMMITTED_SNAPSHOT` (so the public site
keeps reading while editors write), and creates a Windows login for the API
application pool identity.

**Update the login name in the script** to match your environment before
running. The default assumes a local application pool identity:

```sql
DECLARE @AppLogin sysname = N'IIS APPPOOL\LeanPortal-Api';
```

For a separate database server, use a domain service account instead
(`DOMAIN\svc-leanportal`) and grant that account the same roles.

### Schema

By default the API applies pending migrations on start
(`Database:MigrateAndSeedOnStartup`). That is the simplest arrangement and the
one the deployment script assumes.

Where a DBA must apply schema changes by hand:

1. Set `Database:MigrateAndSeedOnStartup` to `false`.
2. Have the DBA run `database/scripts/02-schema.sql` — it is idempotent and
   guards each migration against `__EFMigrationsHistory`.
3. Remove `db_ddladmin` from the application login.

Note that the reference content is seeded by the same start-up path. If you
disable it, the database will have a schema but no content: run the API once
against an empty database with seeding enabled, then turn it off.

Regenerate the script whenever a migration is added:

```powershell
dotnet ef migrations script --idempotent `
  --project backend\src\LeanPortal.Infrastructure `
  --startup-project backend\src\LeanPortal.Api `
  --output database\scripts\02-schema.sql
```

### Bringing a freshly seeded database up to the reviewed content

Seeding produces the reference content, not the content the portal was reviewed
and signed off in: the home page shows more bands and in a different order, ten
site settings differ, and the menu labels are the earlier ones.

`database/scripts/03-sync-content.sql` closes that gap. It touches only page
blocks, site settings and menu items — never users, uploads, enquiries or the
audit log — and refuses to run if any page a menu points at is missing.

```powershell
sqlcmd -S <server> -d LeanPortal -i database\scripts\03-sync-content.sql
```

Take a backup first, read the `-- CHECK:` comments (they mark menu items whose
destination looks unintended), and recycle the `LeanPortal-Api` application pool
afterwards, because settings are cached. Running it a second time changes
nothing further, except that the menus are rebuilt with new identities.

---

## 4. Provision IIS

From an elevated PowerShell session on the web server:

```powershell
.\deploy\scripts\Setup-IIS.ps1 `
    -SiteName LeanPortal `
    -SiteRoot C:\inetpub\LeanPortal `
    -HostName leannew.qci.org.in
```

This creates:

- `C:\inetpub\LeanPortal` and `…\api`, with `wwwroot\uploads` and `logs`
- Application pools `LeanPortal` and `LeanPortal-Api`, both **No Managed Code**,
  always running, with a nightly 03:00 recycle rather than the default rolling
  29-hour one
- The site, the `/api` sub-application, and least-privilege ACLs — read across
  the site, modify only on `uploads` and `logs`
- Room for a crowd: a request queue of 20,000 per pool (IIS's default of 1,000
  answers a burst with 503s) and 20,000 concurrent requests across the server
  (default 5,000). See [section 10a](#10a-capacity-10000-visitors-at-once).
  Re-running the script on an existing server applies these without touching
  anything else.

If the certificate is already installed, add TLS at the same time — this hands
over to `Enable-Https.ps1`, and binds it without turning the redirect on, since
nothing has been deployed yet:

```powershell
.\deploy\scripts\Setup-IIS.ps1 -SiteName LeanPortal -SiteRoot C:\inetpub\LeanPortal `
    -HostName leannew.qci.org.in -CertificateThumbprint "A1B2C3…"
```

Otherwise leave it, deploy, and do TLS afterwards — see [section 7](#7-tls-and-the-redirect-to-https).

---

## 5. Configuration

Copy the template and fill it in:

```powershell
Copy-Item .\deploy\config\appsettings.Production.template.json `
          C:\inetpub\LeanPortal\api\appsettings.Production.json
```

Three values **must** be set before the API will start in production:

| Setting | Notes |
|---|---|
| `ConnectionStrings:DefaultConnection` | Prefer `Integrated Security=true` so no password is stored |
| `Jwt:Key` | At least 32 bytes. The API refuses to start outside Development without it. |
| `Seed:AdminPassword` | Used once, to create the first administrator. **Required**: outside Development the API will not create an administrator without it, and logs an error instead. The development default is published in the repository, so it is never used in production. |

Go-Live.ps1 generates the signing key with a cryptographic random number
generator. To make one by hand, use the same - not `Get-Random`, which is
predictable:

```powershell
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
```

### Environment

Set the environment for the API application pool. Either add it in IIS Manager
(Configuration Editor → `system.applicationHost/applicationPools` →
`environmentVariables`), or:

```powershell
Import-Module WebAdministration
Add-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
  -filter "system.applicationHost/applicationPools/add[@name='LeanPortal-Api']/environmentVariables" `
  -name "." -value @{name='ASPNETCORE_ENVIRONMENT'; value='Production'}
```

Prefer environment variables for the secrets — they keep the values out of the
file system entirely:

```
ConnectionStrings__DefaultConnection
Jwt__Key
Seed__AdminPassword
```

### Sign-in security

Nothing to configure, but worth knowing before go-live:

| What | Behaviour |
|---|---|
| Verification on sign-in | The console's sign-in page asks for the same challenge as the enquiry form, before the password is checked. With account lockout and the `login` rate limit, a script cannot try passwords at any useful speed. |
| An accessible challenge | The challenge is a picture of five characters by default. **"Can't read the characters? Answer a question instead"** switches to a written sum ("What is 8 minus 3?") that a screen reader reads out — GIGW and WCAG 1.1.1 both require a CAPTCHA to offer a form that does not depend on sight. Each challenge is good for one attempt and five minutes. |
| Idle sign-out | After **30 minutes** without activity the console signs itself out and shows the sign-in page with a notice. The server enforces the same limit: a session left idle cannot be renewed (`Jwt:SessionIdleMinutes`, default 30). Access tokens last 15 minutes (`Jwt:AccessTokenMinutes`). Change both together if the Ministry asks for a different limit — the console's timer is `IDLE_MINUTES` in `idle-signout.service.ts`. |

---

## 5a. Enquiry mail

Enquiries are **not** read in the admin console. Each one is saved to the database
and sent to the implementing agency the visitor chose, so mail has to work before
go-live or enquiries will sit in the database with nobody told.

### Configure it in the console

Everything is on the **Enquiry mail** screen: the portal's mail server, each
agency's inbox, and - optionally - a mail server of an agency's own, so its
enquiries leave from its own mailbox. Each has a **Test** button that sends through
exactly the route an enquiry would take. **Use Outlook / Microsoft 365 settings**
fills in the server, port and encryption below.

| Field | Microsoft 365 / Outlook |
| --- | --- |
| Mail server | `smtp.office365.com` |
| Port | `587` |
| Encryption | `StartTls` |
| Sign-in name | the mailbox the portal signs in as |
| Password | see the note below |
| Send as | the address enquiries come from |

The password is encrypted before it is stored, using a key kept outside the
database, and is never sent back to the browser - the field shows empty with a
note that one is saved. Leave it empty when saving to keep the stored one; type a
new one to replace it. The same holds for an agency's own server.

The keys live in `keys/` beside the application, or wherever
`DataProtection:KeyPath` points. Back that folder up with the site: without it the
password cannot be decrypted and has to be entered again.

### Microsoft 365 in particular

Two things catch people out:

- **Basic authentication for SMTP is off by default.** An account with multi-factor
  authentication needs an **app password**, or SMTP AUTH has to be enabled for that
  mailbox in the Microsoft 365 admin centre. The portal signs in with a user name
  and password; it does not do OAuth.
- **"Send as" must be permitted.** Microsoft 365 rejects mail whose From address the
  signed-in mailbox is not allowed to send as. Either send as the mailbox itself, or
  grant it Send As permission on the address you want to use.

If the organisation would rather not hold a mailbox password at all, Microsoft 365
"direct send" or an internal relay works too: leave the sign-in name and password
empty and point the server at the relay.

### Or configure it on the server

An `Smtp` section in `appsettings.Production.json` still works and is used whenever
the portal's server is left empty on the Enquiry mail screen - useful for setting mail up before anyone can sign in,
or to keep the password out of the database entirely:

```json
"Smtp": {
  "Host": "smtp.office365.com",
  "Port": 587,
  "EnableSsl": true,
  "User": "lean-portal@msme.gov.in",
  "Password": "",
  "FromAddress": "no-reply@lean.msme.gov.in",
  "FromName": "MSME Competitive (LEAN) Scheme",
  "CopyTo": ""
}
```

### Each agency's inbox, and its own server

On the same screen, per agency: the **Enquiries inbox** (it falls back to the
agency's published e-mail, then to `contact.email`, and is never shown on the
site), and **Sent from its own mail server** with that server's details. An agency
without its own server uses the portal's. The ministry's blind copy is kept either
way. For QCI, while its Zoho Desk connection is on, mail is only the fallback.

### Asking which agency

The form asks "Who is your enquiry for?" while **Ask which implementing agency the
enquiry is for** is on (Settings → Contact) and more than one agency is active.
Otherwise it does not ask, and every enquiry goes to the agency named in **Agency
enquiries go to when the question is not asked** - handled as that agency's are,
Zoho for QCI, mail for NPC - or, with none named, to the general enquiry address.

To retrieve enquiries directly - after a relay outage, say - `GET /api/admin/enquiries`
and its `/export` still work for a signed-in administrator; there is simply no
screen for them.

---

## 5b. Data protection keys

The mail password set in the console is stored encrypted, and the keys live in a
folder on the server rather than in the database. Two things follow, both already
handled by the scripts but worth knowing:

- `Setup-IIS.ps1` creates `api\keys` and grants the API app pool Modify on it.
  Without write access the keys are regenerated on every recycle and the stored
  password stops decrypting.
- `Deploy-LeanPortal.ps1` excludes `keys` from the mirror, along with `uploads`
  and `logs`. A `robocopy /MIR` would otherwise delete them on every deployment.

Back this folder up with the site. If it is lost, the mail password simply has to
be entered again - nothing else depends on it.

---

## 5c. Branding

**Branding** in the console: the four logos - header left, header right, footer,
and an optional second footer logo - each with the address it opens and its
description for screen readers, and the colour theme.

- A logo link that is a page of the site (`/about-scheme`) opens in place; a full
  `https://` address opens in a new tab and says so. Anything else is refused.
- Logos upload as PNG, JPG or WebP. SVG is refused - it can carry script - so the
  original SVG lockups are kept, and **Back to the original** restores them.
- Seven preset themes, each measured to WCAG AA, or **Custom** with two colours
  that must pass the same measure: white text on the accent at 4.5:1, on the dark
  colour at 7:1. The theme is served as `/api/site/theme.css`, linked from the
  page head so pages draw in it from the first paint, and it stands aside for a
  visitor who turns on high contrast. The site's `web.config` needs nothing for it.

Banner images are content, not theme: an illustration drawn in its own colours
keeps them whatever theme is chosen. Replace it under **Banners**.

## 5d. Helpdesk and integrations

### QCI's enquiries in Zoho Desk

**Helpdesk (Zoho)** in the console. Enquiries for the agency chosen there - QCI -
are raised as Zoho Desk tickets; NPC's go by e-mail as before (section 5a).

What it needs from QCI, issued **for LEAN**:

| Field | Notes |
| --- | --- |
| Organisation id | Sent as the `orgId` header |
| Department id | The department LEAN's tickets land in |
| Client id, client secret, refresh token | From Zoho's API console. The secret and token are stored encrypted and never shown again |

The credentials in the SAMAR integration document are SAMAR's. Do not reuse them:
tickets would land in SAMAR's department, and two credential sets from that
document are already in circulation.

Save it switched off, press **Test the saved connection** (it reads the department
back and raises nothing), then switch it on. The **ticket fields** box is the JSON
added to every ticket - the custom fields go in `cf` - and **Preview a ticket**
shows exactly what Zoho will be sent. The **grievance matrix** is the four linked
lists the contact form shows when QCI is chosen; each option must match the value
in Zoho's picklist exactly.

An enquiry is always saved in the database first. If Zoho refuses it, it is
retried for about four hours and then e-mailed to QCI's enquiries inbox, marked
ACTION NEEDED. **Retry now** re-sends everything waiting once a fault is fixed.

### Certificate verification, certified units, the chatbot

**Integrations** in the console. Each can be a link, a page in a frame, the
provider's embed code, or an API; the chatbot can also stay **built in**, answering
from the portal's pages, FAQs, documents (including the text inside PDFs and
Office files), notices and listings.

- An **API** is called by the server, never the browser. The key header is stored
  encrypted. **Run test** shows what came back, so the column paths can be read off it.
- **Embed code** runs in a sandboxed frame with an origin of its own - it cannot
  read the console's session. The site's `web.config` deliberately removes its own
  Content-Security-Policy from `/api/site/integrations` so that page's own policy applies.

### Outbound calls

Both call addresses typed into the console, so the server refuses to connect to
private, loopback and link-local addresses - which is what keeps a hijacked
administrator account away from the VM's metadata service. The outbound firewall
must allow HTTPS to `accounts.zoho.in`, `desk.zoho.in` and whichever provider the
integrations are pointed at. Never set `Integrations:AllowPrivateNetworks` outside
development.

---

## 6. Deploy

```powershell
.\deploy\scripts\Deploy-LeanPortal.ps1 `
    -SiteRoot C:\inetpub\LeanPortal `
    -AppPoolName LeanPortal `
    -ApiAppPoolName LeanPortal-Api
```

The script publishes both projects, keeps the release it is about to replace
under `C:\inetpub\LeanPortal.releases` (the last three), stops the pools, mirrors
the new release into place - never touching `uploads`, `logs`, `keys` or
`appsettings.Production.json` - reapplies write permissions, restarts the pools
and health-checks `/api/health`.

A server that cannot build (no SDK, no Node.js, no internet for npm and NuGet)
deploys a package instead: run `deploy\scripts\Build-Package.ps1` on a machine
that can, copy the zip across, and pass `-PackagePath`. `Rollback-LeanPortal.ps1`
puts the previous release back. `-WhatIf` shows what would happen without
touching the server.

### First start

The first request after a deployment applies any pending migrations and, on an
empty database, seeds the content. It will take a few seconds. Watch
`C:\inetpub\LeanPortal\api\logs\lean-portal-<date>.log`.

Then sign in at `https://<host>/admin` with the seeded administrator and change
the password — the console forces this before anything else opens.

---

## 7. TLS and the redirect to HTTPS

One script does all of it — installs the certificate and its issuer chain, binds
443, proves the handshake, and only then turns the redirect on.

The portal uses QCI's wildcard certificate, `*.qci.org.in` (GoDaddy, valid to
**27 January 2027**), which covers `leannew.qci.org.in`. Copy the `.pfx` and the
GoDaddy intermediate bundle (`gd_dv-r1-g2_iis_intermediates.p7b`) from the
certificate folder to `C:\certs` on the server, then:

```powershell
.\deploy\scripts\Enable-Https.ps1 `
    -SiteName LeanPortal `
    -HostName leannew.qci.org.in `
    -SiteRoot C:\inetpub\LeanPortal `
    -PfxPath C:\certs\qci.org.in.pfx `
    -PfxPassword (Read-Host -AsSecureString) `
    -ChainPath C:\certs\gd_dv-r1-g2_iis_intermediates.p7b
```

Type the PFX password at the prompt; it never appears on the command line or in
the shell history. `-ChainPath` imports the intermediates into `LocalMachine\CA`
so IIS sends the full chain — without them some browsers and most API clients
report the certificate as untrusted even though it works on the server.

Once installed, delete the copies in `C:\certs`. The private key is in the
machine store; loose copies of a wildcard key are the most valuable thing on
the server.

Use `-CertificateThumbprint` instead of the two PFX parameters when the
certificate is already in `LocalMachine\My`. Run it again after every renewal;
nothing else needs touching.

> **Handling the certificate files.** The bundle as supplied has the PFX password
> in the PFX's file name, and the private key also as an unencrypted `.key` file.
> Anyone who has had the folder can impersonate every `*.qci.org.in` site until
> January 2027. Keep it off shared drives, mail and source control (it is
> excluded from this repository), and ask QCI's IT team to consider re-keying
> the certificate with a new password.

### Why the ordering matters

A redirect to HTTPS on a site where 443 is not listening takes the site off the
air, and once browsers have cached a permanent redirect it stays off for them
even after the redirect is removed. So the rule is never applied on trust:

- The redirect lives in `deploy/iis/web.frontend.config`, in source control.
- `Deploy-LeanPortal.ps1` strips it out of the deployed copy whenever the site
  has no HTTPS binding, and says so in its output.
- `Enable-Https.ps1` puts it back only after it has opened a TLS connection to
  the local listener, asked for the host by name, and seen the certificate it
  just bound come back.

That last check is a real handshake against `127.0.0.1:443` with SNI, not a
guess from configuration, and it does not depend on DNS, the firewall or the
network security group.

### Two things the script will not do for you

Both are changes to a security boundary, so it reports and leaves them to you:

- **Windows Firewall.** If there is no inbound rule for TCP 443 it prints the
  `New-NetFirewallRule` command to run.
- **The Azure network security group.** Inbound TCP 443 has to be allowed on the
  VM's NSG. That cannot be read from inside the machine — check it in the portal.

It also reports, without changing, whether TLS 1.0, 1.1 and SSL 3.0 are still
enabled in SCHANNEL. A CERT-In or GIGW review will expect TLS 1.2 and above only.

### HSTS

`Strict-Transport-Security` is sent from one place: the site's `web.config`. The
API does not send it — TLS terminates at IIS, and two headers would mean a
browser honours only the first.

It ships at `max-age=86400`, one day, on purpose. A browser that has seen this
header refuses to reach the site over plain HTTP and will not let a visitor
click past a certificate warning for the whole of `max-age`. At a year, one
missed renewal makes the site unreachable for everyone who has ever visited.

Raise it to `31536000` in `deploy/iis/web.frontend.config` and redeploy once a
renewal has been through the cycle successfully. Consider `preload` only after
that, and only knowing it is slow to undo.

### Confirm it from somewhere else

The script's checks all run on the server. From another machine:

1. `https://leannew.qci.org.in/` loads with no certificate warning.
2. `http://leannew.qci.org.in/` redirects to it.
3. `https://leannew.qci.org.in/api/site/sitemap.xml` lists `https://` addresses.

The third is worth doing. The sitemap is generated from the scheme of the
incoming request, and `robots.txt` already points search engines at the `https`
address — if it comes back `http`, crawlers are being handed the wrong ones.

---

## 8. Integration with the transactional LEAN system

The portal is the public face; registration, the LEAN Pledge, handholding and
assessment stay in the existing transactional application. The links between
them are configuration, not code — **Settings → Application** in the admin
console:

| Setting | Points at |
|---|---|
| `app.msmeRegister` / `app.msmeLogin` | MSME registration and sign-in |
| `app.agencyLogin` | QCI / NPC |
| `app.ministryLogin`, `app.dfoLogin` | Ministry and state |
| `app.consultantLogin`, `app.oemLogin` | Consultants and OEMs |

Relative values are resolved against `links.leanApp`. Set that to the origin of
the transactional system.

### Live statistics

The counter strip is editable, and each counter can carry a `SourceQueryKey`.
Where one is set, a scheduled job is expected to refresh `Statistic.Value` from
the transactional database. That job is **not** included here — implement it as
a SQL Agent job or a hosted service once the source schema is confirmed.
Counters without a key are maintained by hand and work as-is.

---

## 9. Backups and retention

| What | How | Frequency |
|---|---|---|
| Database | Full backup | Nightly |
| Database | Transaction log | Every 15 minutes (recovery is FULL) |
| `api\wwwroot\uploads` | File backup | Nightly — **not** in the database |
| `api\appsettings.Production.json` | Secure store | On change |

The uploads folder holds every document and image the CMS serves. A database
restore without it leaves the site with broken links.

Application logs roll daily and are retained for 90 days. The `AuditLogs` table
is never purged by the application — agree a retention period with the Ministry
and archive it on that schedule.

---

## 10. Scaling out

For more than one web node:

1. Point `FileStorage:RootPath` at a UNC share both nodes can write to, and
   grant the application pool identities access to it.
2. Give the pool identities a domain service account so the share and SQL Server
   authenticate consistently.
3. Set `Database:MigrateAndSeedOnStartup` to `false` and apply migrations from a
   single controlled step, so two nodes cannot race to migrate.
4. The API is stateless — JWTs are self-contained and the output cache is
   per-node — so no session affinity is required.

---

## 10a. Capacity: 10,000 visitors at once

The portal is configured for 10,000 people using it at the same time.

| Layer | Setting | Where |
|---|---|---|
| IIS | Request queue 20,000 per pool; 20,000 concurrent requests | `Setup-IIS.ps1` (`-QueueLength`, `-ConcurrentRequestLimit`) |
| .NET | 200 worker threads ready from start-up, instead of adding them one a second under sudden load | `LeanPortal.Api.csproj` (`ThreadPoolMinThreads`) |
| SQL Server | Connection pool of 20–400 (default 100) | `Max Pool Size` in the connection string template |
| API | Public content served from the output cache (60 s – 5 min, cleared the moment an editor publishes), so most page views do not reach the database at all | `Program.cs` |
| Visitor counter | Counted in memory and written once every 5 seconds, instead of one UPDATE per visitor on the same row | `VisitorCounter.cs` |
| Static files | Hashed bundles cached by browsers for a year; gzip compression of scripts, styles, JSON and SVG | `web.frontend.config` switches it on; `Setup-IIS.ps1` sets the types server-wide |

**Rate limits** are per client address, per minute (`RateLimiting` in
`appsettings.Production.json`):

| Setting | Limit | Covers |
|---|---|---|
| `PublicPerMinute` | 600 | Reading the site. Generous because a ministry office or a campus reaches the site through one address. |
| `CaptchaPerMinute` | 30 | Drawing a verification challenge |
| `FormsPerMinute` | 5 | Sending an enquiry, subscribing |
| `LoginPerMinute` | 8 | Console sign-in |

If QCI puts a load balancer or WAF in front of the server, every visitor will
arrive from its address and share one allowance. In that case list its address
under `ForwardedHeaders:KnownProxies` in `appsettings.Production.json`, e.g.
`"ForwardedHeaders": { "KnownProxies": ["10.0.0.5"] }`, so the real client
address is taken from `X-Forwarded-For`. Only list a device that sets that
header itself: a trusted proxy's header is believed, so listing anything else
lets a visitor claim any address.

### Measured

On a development laptop (8 cores, with the load generator, the API and SQL
Server Express all on the same machine), 10,000 simulated visitors arriving
within ten seconds and browsing with 2–8 seconds between pages:

| Requests | Rate | Median | 95th percentile | 99th percentile | Errors |
|---|---|---|---|---|---|
| 470,908 in 68 s | ~8,000 a second sustained | 6 ms | 14 ms | 29 ms | **0** |

The visitor counter recorded exactly 10,000 new visits. A production server with
SQL Server on its own machine will have more headroom than this.

### Test it on the server

`deploy/loadtest` is a small, dependency-free load generator. From a machine
*other than the server* (on the same one, the two compete and the figures measure
the machine rather than the portal):

```powershell
cd deploy\loadtest
dotnet run -c Release -- https://leannew.qci.org.in/api 10000 120 2000 8000
```

Arguments: API address, visitors, seconds, then the shortest and longest pause
between pages in milliseconds. All the traffic comes from one address, so run it
against a staging copy with `RateLimiting__PublicPerMinute` raised — against
production it will be rate-limited within seconds, which is the limiter working.

---

## 11. Troubleshooting

**HTTP 500.30 or 500.31 on `/api`**
The API failed to start. Check `api\logs\`. Most often `Jwt:Key` is missing or
shorter than 32 bytes — the API deliberately refuses to start rather than sign
tokens with a weak key.

**HTTP 500.19**
The URL Rewrite module is missing, so IIS cannot parse the `<rewrite>` section.

**Angular routes 404 on refresh**
The rewrite rule is not applying. Confirm `web.config` is in the site root and
that URL Rewrite is installed.

**Uploads fail with 500**
The application pool identity cannot write to `wwwroot\uploads`. Re-run the
deployment script, which reapplies the ACL, or:

```powershell
icacls C:\inetpub\LeanPortal\api\wwwroot\uploads /grant "IIS AppPool\LeanPortal-Api:(OI)(CI)M" /T
```

**Login succeeds but every admin call returns 401**
The system clocks of the web server and the client have drifted. Token validation
allows one minute of skew. Check the Windows Time service.

**Database connection fails with a Windows login**
The application pool identity is not reaching SQL Server. For a remote instance,
a local `IIS AppPool\…` identity will not authenticate — switch the pool to a
domain service account and grant that account the database roles.

**Deployment fails copying DLLs**
An application pool did not stop. The script waits 60 seconds and copies nothing
if a pool is still running; stop the pool by hand and run the deployment again.

**The site works on the server but nothing answers on 443 from outside**
The handshake check in `Enable-Https.ps1` runs against `127.0.0.1`, so it passes
whether or not the port is reachable. Windows Firewall and the Azure network
security group both have to allow inbound TCP 443.

**Browsers say the certificate is not trusted, but it works on the server**
An intermediate is missing. The chain builds on the server because the issuer is
already in its store. Import the issuer bundle into `LocalMachine\CA` and re-run
`Enable-Https.ps1` — it reports the chain depth it can build.

**Everything redirects to HTTPS and HTTPS is broken**
Do not simply remove the redirect: browsers have cached a permanent redirect, and
HSTS will hold them on HTTPS regardless. Fix the certificate. To buy time, stop
the site rather than leaving a half-working one — a connection error is
recoverable, a cached bad state is not.

**Visitors see "Too many requests"**
They have hit a rate limit. If many people share one address — an office, or a
load balancer in front of the server — raise `RateLimiting:PublicPerMinute`, or
set `ForwardedHeaders:KnownProxies` so the real address is used (section 10a).

**503 Service Unavailable under load**
The IIS request queue is full. Re-run `Setup-IIS.ps1` on the server: it sets the
queue to 20,000 and leaves everything else as it is.

**The sitemap advertises `http://` addresses**
`Request.Scheme` reached the API as HTTP. The API runs in process, so IIS is the
server and this should not happen; if it does, check that no proxy in front is
terminating TLS and forwarding without `X-Forwarded-Proto`.

---

## 12. Post-deployment checklist

- [ ] `https://<host>/` serves the landing page over TLS, with no warning, from a
      machine that has never seen the certificate
- [ ] `http://<host>/` redirects to it
- [ ] Inbound TCP 443 allowed in both Windows Firewall and the Azure NSG
- [ ] `https://<host>/api/health` returns `healthy`
- [ ] `https://<host>/api/site/sitemap.xml` lists `https://` addresses
- [ ] A renewal reminder set for a month before the certificate expires
      (the QCI wildcard expires **27 January 2027**)
- [ ] The certificate copies in `C:\certs` deleted after installation
- [ ] HSTS raised from one day to a year — *after* the first renewal has worked
- [ ] A deep link such as `/about-scheme/financial-assistance` survives a refresh
- [ ] `/admin` sign-in works — verification challenge, then the forced
      password change — and the written-question alternative works
- [ ] `https://<host>/robots.txt` points at the `leannew.qci.org.in` sitemap, and
      the sitemap is submitted in Google Search Console and Bing Webmaster Tools
- [ ] A test upload through the media library succeeds and the file is served
- [ ] The enquiry form submits and the chosen agency receives the mail
      (enquiries are stored in the database, not shown in the console)
- [ ] Placeholder photography (hero backgrounds, gallery, partner tiles) replaced
- [ ] Seeded content reviewed against the current scheme guidelines
- [ ] Backups scheduled for both the database and the uploads folder
- [ ] `Seed:AdminPassword` removed from configuration after first start
