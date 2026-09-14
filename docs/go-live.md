# Go-live — leannew.qci.org.in

The step-by-step for putting the portal live on the Windows server, from the code
downloaded from GitHub. One script does the work; this page says what to prepare,
what to type, and what to do afterwards.

Everything is run on the **production server**, in an **elevated** PowerShell
window (right-click Windows PowerShell → *Run as administrator*).

---

## 1. Before you start

### What you need

| Item | Where from |
|---|---|
| The code | GitHub → *Code* → *Download ZIP*, extracted to `E:\Lean-Website-Final-main` |
| The QCI certificate | The `.pfx` file for `*.qci.org.in` and GoDaddy's intermediate bundle (`gd_dv-r1-g2_iis_intermediates.p7b`), both in `E:\certs` |
| The PFX password | From QCI IT. Typed when asked; never written anywhere. |
| A SQL Server administrator | Your Windows account as a sysadmin on SQL Server, or the `sa` login |
| A DNS record | `leannew.qci.org.in` → this server's public address (QCI IT) |
| Port 443 and 80 open | On any network firewall or WAF in front of the server (QCI IT) |

> **Keep the certificate out of the code folder.** Move the `.pfx` to `E:\certs`
> before you start. Its password is written in its file name, and anything in
> the code folder can end up copied or zipped with it.

### Software to install first

| Software | Notes |
|---|---|
| Windows Server 2019 or later | |
| SQL Server 2019 or later | Express works; Standard is better for a public portal (backup compression, more memory) |
| **IIS** | Installed by the script in step 3 below — do this *before* the Hosting Bundle |
| [.NET 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0) | Install **after** IIS. Then run `iisreset`. |
| [URL Rewrite 2.1](https://www.iis.net/downloads/microsoft/url-rewrite) | |
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and [Node.js 24 LTS](https://nodejs.org) | Only to build on this server. It also needs internet access to npm and NuGet. If it has neither, see [Building elsewhere](#building-elsewhere). |

---

## 2. Open PowerShell in the code folder

```powershell
cd E:\Lean-Website-Final-main
Get-ChildItem -Recurse -Include *.ps1,*.psd1 | Unblock-File
Set-ExecutionPolicy -Scope Process Bypass -Force
```

Files from a downloaded zip are marked as coming from the internet, and Windows
will not run them until they are unblocked. `-Scope Process` changes the policy
for this window only.

---

## 3. Install IIS

```powershell
.\deploy\Go-Live.ps1 -Step Prerequisites
```

This installs the IIS role services the portal needs. When it asks, install the
**.NET 10 Hosting Bundle** and **URL Rewrite 2.1**, run `iisreset`, and carry on.

---

## 4. Check the settings

Open `deploy\golive.settings.psd1` in Notepad. The defaults are for this server:

| Setting | Default | Change it if |
|---|---|---|
| `HostName` | `leannew.qci.org.in` | — |
| `SiteRoot` | `C:\inetpub\LeanPortal` | the site should live on another drive |
| `SqlServer` | `localhost` | SQL Server is a named instance (`localhost\SQLEXPRESS`) or another machine |
| `SqlAdminAuth` | `Windows` | you will sign in to SQL Server as `sa` — set `Sql` |
| `SqlAppAuth` | `AppPoolIdentity` | SQL Server is on another machine — set `SqlLogin` |
| `AdminEmail` | `lean-admin@qci.org.in` | the first administrator should be someone else |
| `BackupRoot` | `E:\LeanPortalBackups` | backups should go elsewhere |
| `DisableLegacyTls` | `$false` | you want TLS 1.0/1.1 switched off for the whole server (audits expect it; needs a restart) |

No password goes in this file.

---

## 5. Check the server

```powershell
.\deploy\Go-Live.ps1 -Step Preflight
```

Changes nothing. Every line should be `[ OK ]`. Fix any `[FAIL]` and run it again.
`[WARN]` lines are worth reading but do not stop the go-live.

---

## 6. Go live

```powershell
.\deploy\Go-Live.ps1
```

It runs every step in order and stops at the first problem, telling you how to
carry on from there. It asks three things:

1. **The SQL administrator** — only if `SqlAdminAuth` is `Sql`.
2. **The first CMS administrator's password** — at least 12 characters, with a
   capital, a small letter, a digit and a symbol. Used once to create the
   account, then removed from the server. It must be changed at first sign-in.
3. **The PFX password.**

| Step | What it does |
|---|---|
| Preflight | The checks from step 5 |
| Prerequisites | IIS role services |
| IIS | The `LeanPortal` site bound to `leannew.qci.org.in`, the `LeanPortal` and `LeanPortal-Api` pools, `/api`, capacity for 10,000 visitors, firewall 80/443; stops the unused *Default Web Site* |
| Database | Database `LeanPortal`, and the portal's own least-privilege login |
| Settings | `C:\inetpub\LeanPortal\api\appsettings.Production.json` — connection string, a generated signing key, the administrator password — readable only by administrators and the portal |
| Deploy | Builds, copies into place, starts, and confirms the database and administrator were created |
| Https | Imports the certificate and intermediates, binds 443, proves the handshake, then turns on the HTTP→HTTPS redirect |
| Verify | Redirect, certificate, security headers, deep links, API, sitemap, robots.txt, and that nothing private can be downloaded |
| Backups | Nightly full backup and 15-minute log backups as scheduled tasks, and a first backup now |

The whole run is recorded in `deploy\logs\golive-<date>.log`.

---

## 7. After the script

1. **Sign in** at `https://leannew.qci.org.in/admin` as the administrator and set
   a new password.
2. **Delete the `.pfx`** from `E:\certs` (keep the original safely off the server).
3. **Enquiry mail** — set the mail server and each agency's inbox; send a test.
4. **Helpdesk (Zoho)** — enter QCI's LEAN credentials; *Test the saved connection*.
5. **Settings → Contact** — replace the helpline placeholder `1800-XXX-XXXX`.
6. **Documents** — the starter content links to seven placeholder documents
   (scheme guidelines, brochure, registration process, LEAN pledge, consultant
   guidelines, the additional-incentive circular and the assessment checklist)
   that are not part of the code. Upload the real files, or those download links
   will not work.
7. **Users** — create a named account for each editor; do not share the administrator.
8. **From outside the network**, open `https://leannew.qci.org.in` — no warning.
9. **Google Search Console** — submit `https://leannew.qci.org.in/api/site/sitemap.xml`.
10. **Diary**: the certificate expires on **27 January 2027** — renew a month
    before, then run `.\deploy\Go-Live.ps1 -Step Https, Verify`.
11. **Copy `E:\LeanPortalBackups` off the server** regularly. A backup on the same
    disk does not survive losing the disk.

---

## Updating the site later

Download the new code to a fresh folder, then from that folder:

```powershell
Get-ChildItem -Recurse -Include *.ps1,*.psd1 | Unblock-File
Set-ExecutionPolicy -Scope Process Bypass -Force
.\deploy\Go-Live.ps1 -Step Deploy, Verify
```

Copy your edited `deploy\golive.settings.psd1` across first if you changed it.
The Deploy step backs up the database, keeps the current release for rollback,
and never touches uploads, logs, keys or the settings file.

### Rolling back

```powershell
.\deploy\scripts\Rollback-LeanPortal.ps1 -SiteRoot C:\inetpub\LeanPortal -AppPoolName LeanPortal -ApiAppPoolName LeanPortal-Api
```

Restores the code of the previous release. If that release changed the database,
restore the backup taken just before it too (below).

---

## Building elsewhere

When the server has no .NET SDK, no Node.js or no internet access, build on any
Windows computer that has them:

```powershell
.\deploy\scripts\Build-Package.ps1
```

Copy the zip it makes (`deploy\packages\LeanPortal-package-<date>.zip`) to the
server, and add `-PackagePath` to the go-live or update command:

```powershell
.\deploy\Go-Live.ps1 -PackagePath E:\LeanPortal-package-20260914-1030.zip
```

The package holds no settings and no secrets.

---

## Restore from a backup

Backups are in `E:\LeanPortalBackups`: `database\` (`*_full_*.bak` nightly,
`*_log_*.trn` every 15 minutes) and `files\` (uploads, keys and the settings file).

**The database** — stop the site, then in SQL Server Management Studio (or
`sqlcmd`), restore the latest full backup and every log backup taken after it,
in order:

```sql
RESTORE DATABASE [LeanPortal] FROM DISK = N'E:\LeanPortalBackups\database\LeanPortal_full_20260914-020000.bak'
    WITH NORECOVERY, REPLACE;
RESTORE LOG [LeanPortal] FROM DISK = N'E:\LeanPortalBackups\database\LeanPortal_log_20260914-021500.trn' WITH NORECOVERY;
-- ...each later log backup, in time order...
RESTORE DATABASE [LeanPortal] WITH RECOVERY;
```

```powershell
Stop-WebAppPool LeanPortal-Api     # before the restore
Start-WebAppPool LeanPortal-Api    # after it
```

**The files** — unzip the `files\LeanPortal_files_<date>.zip` from the same night:
`uploads` into `C:\inetpub\LeanPortal\api\wwwroot\uploads`, `keys` into
`C:\inetpub\LeanPortal\api\keys`. Restore the keys together with the database:
without them the mail and Zoho passwords saved in the CMS cannot be read, and
have to be entered again.

---

## When something goes wrong

| What you see | What to do |
|---|---|
| *...cannot be loaded because running scripts is disabled* | Step 2: `Unblock-File` and `Set-ExecutionPolicy -Scope Process Bypass -Force` |
| `.NET 10 Hosting Bundle is not installed` after installing it | It went in before IIS. Run its installer again, choose **Repair**, then `iisreset` |
| `Cannot reach https://registry.npmjs.org` | The server has no internet for the build — [build elsewhere](#building-elsewhere) |
| `Port 80 is taken by another program` | Another web server (Apache, Tomcat, Skype for Business) is running; stop it or move it |
| Deploy: *did not answer /api/health* | The script prints the end of the API log. Most often the SQL login: check the Database step ran, and that SQL Server allows the connection |
| HTTP Error 500.19 | URL Rewrite is not installed |
| HTTP Error 500.30 / 500.31 | The API could not start: `C:\inetpub\LeanPortal\api\logs`, then Event Viewer → Application → *IIS AspNetCore Module V2* |
| Verify: certificate not trusted | The intermediate bundle (`.p7b`) was not found — put it in `E:\certs` and run `-Step Https, Verify` |
| The site works on the server but not from outside | DNS, or a firewall/WAF in front of the server — QCI IT |
| A backup task shows a non-zero result | `E:\LeanPortalBackups\backup.log` says why |

The detailed reference for each part — architecture, capacity, HSTS, scaling out
— is [deployment.md](deployment.md).
