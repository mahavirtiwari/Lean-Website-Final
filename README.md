# MSME Competitive (LEAN) Scheme Portal

A replacement public website and content management system for the MSME
Competitive (LEAN) Scheme of the Ministry of Micro, Small and Medium
Enterprises, Government of India.

The existing portal at [lean.msme.gov.in](https://lean.msme.gov.in) has no CMS:
every content change needs a developer. This rebuild puts the entire public site
— pages, navigation, the composition of the landing page itself, documents,
notices, programmes and scheme reference data — behind an editorial console.

| | |
|---|---|
| **Front end** | Angular 22, standalone components, signals, zoneless change detection |
| **Back end** | ASP.NET Core 10 Web API, EF Core 10, ASP.NET Identity, JWT |
| **Database** | SQL Server (2019 or later; developed against SQL Server 2025) |
| **Hosting** | Windows Server, IIS 10 with the .NET 10 Hosting Bundle |

---

## Contents

- [What is in the box](#what-is-in-the-box)
- [Repository layout](#repository-layout)
- [Running it locally](#running-it-locally)
- [Design](#design)
- [Content model](#content-model)
- [Security](#security)
- [Accessibility](#accessibility)
- [Deployment](#deployment)
- [Further documentation](#further-documentation)

---

## What is in the box

### Public site

- **Composable landing page.** The home page is assembled from ordered blocks —
  hero carousel, quick actions, welcome and video, animated counters, scheme
  components, documents and notices, ministry message, scheme levels, login
  portals, initiatives, success stories, useful links, call to action and a
  contact strip. Editors re-order, retitle, hide or remove any band without a
  release.
- **Scheme content** carried over from the existing portal: introduction,
  objective, the six scheme components, the LEAN Pledge and the three
  implementation levels, coverage and eligibility, e-certification and financial
  assistance, how to register, and benefits to MSMEs.
- **Media centre**: news, announcements, circulars and tenders, with a scrolling
  ticker; a downloads library; a photo gallery with a keyboard-navigable
  lightbox.
- **Programme listings** for awareness programmes and assessor / consultant
  training, filterable by state, district, agency and status.
- **20 FAQs**, an enquiry form with reference numbers, a sitemap, screen-reader
  guidance and the full set of government policy pages.

### Admin console

- Dashboard, page manager with a full editor and editorial workflow
  (draft → review → published → archived).
- The home page **block builder**.
- News and notices, navigation across five menu locations, gallery albums, a
  media library with upload, enquiry triage with CSV export, grouped site
  settings, user and role administration, and an immutable audit log.
- Nine further resources — banners, FAQs, documents, statistics, login portals,
  scheme levels and components, success stories, partners, programmes — are
  served by a single configuration-driven management screen, so they all behave
  identically.

---

## Repository layout

```
├── backend/                        ASP.NET Core solution
│   ├── src/
│   │   ├── LeanPortal.Domain/          Entities, enums, identity
│   │   ├── LeanPortal.Application/     DTOs, contracts, mapping, interfaces
│   │   ├── LeanPortal.Infrastructure/  EF Core, migrations, seed, services
│   │   └── LeanPortal.Api/             Controllers, auth, middleware
│   └── LeanPortal.slnx
│
├── frontend/lean-portal/           Angular workspace
│   └── src/
│       ├── app/core/                   Models, services, interceptors, guards
│       ├── app/shared/                 Icons, pipes, shared components
│       ├── app/layout/                 Public and admin shells
│       ├── app/features/public/        Public screens
│       ├── app/features/admin/         CMS screens
│       └── styles/                     Design tokens and global styles
│
├── database/scripts/               Provisioning and generated schema
├── deploy/                         IIS configuration and PowerShell scripts
└── docs/                           Architecture, deployment and editor guides
```

---

## Running it locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20 or later](https://nodejs.org/)
- SQL Server, SQL Server Express, or LocalDB

### 1. Start the API

```bash
cd backend/src/LeanPortal.Api
dotnet run
```

On first start the API creates the `LeanPortal` database, applies the
migrations and seeds the full reference content set. It listens on
`http://localhost:5199`, with Swagger at `/swagger`.

The default connection string targets `.\SQLEXPRESS`, so the database sits on a
real SQL Server instance you can browse in SSMS and back up. Any SQL Server,
SQL Server Express or LocalDB instance will do - point it elsewhere by editing
`appsettings.Development.json`, or:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=LeanPortal;Trusted_Connection=True;TrustServerCertificate=True"
```

### 2. Start the front end

```bash
cd frontend/lean-portal
npm install
npm start
```

The site is served at `http://localhost:4200` and the admin console at
`http://localhost:4200/admin`.

### 3. Sign in

The seeded administrator is created on first start:

| | |
|---|---|
| E-mail | `admin@lean.msme.gov.in` |
| Password | `ChangeMe@Lean2026` |

The account is flagged to change its password at first sign-in, so the console
will not open until a new password is set. Override the initial password with
`Seed:AdminPassword`; the default is only used when that setting is empty, and
the API logs a warning when it falls back to it.

---

## Design

The interface follows two approved reference designs.

**Landing page** — the band structure of the reference layout: utility strip,
masthead, hero carousel, overlapping quick-action cards, welcome and video
split, counter strip, icon grid, documents and notices, message with priority
tiles, level cards, portal tiles, testimonials, link strip, call to action.

**Internal pages** — the reference service-page shell: a dark title banner, a
sticky breadcrumb rule, a left rail carrying section navigation and support
widgets, and a prose column for CMS-authored HTML.

The palette departs from both references deliberately, at the client's request:
a **petrol teal accent (`#0f7989`)** over **neutral graphite (`#25333f`)** rather
than crimson over near-black navy. The exact shade is measured rather than
eyeballed — it reaches 5.10:1 against white, so white text on a primary button
or the utility bar clears WCAG AA at body size with room to spare. A cool accent
is doing real work here: every hue must be darkened until it clears 4.5:1 on
white, and warm hues fall much further before they get there — orange only
passes at 39% lightness and amber at 34%, which is where they stop reading as
orange and start reading as brown, whereas teal passes at 30% and still reads as
teal. Two separate on-dark tints keep the accent legible on the darker bands,
one for display sizes and one that meets AA for small text.

Full-page comparisons of every scheme that was considered are in
`docs/palette-previews/`.

Everything reads from custom properties in
`frontend/lean-portal/src/styles/_tokens.scss`, so a rebrand is a single-file
change. The same file defines the high-contrast theme.

---

## Content model

20 entities behind the CMS. The ones worth knowing:

| Entity | Purpose |
|---|---|
| `Page` | A content page. Hierarchical slugs (`about-scheme/objective`) drive both the URL and the breadcrumb trail. |
| `PageBlock` | One band on a Blocks-template page. This is what makes the landing page editable. |
| `MenuItem` | Navigation, across five locations, two levels deep. |
| `Post` | News, announcements, circulars, tenders and press releases. |
| `SchemeLevel` / `SchemeComponent` | The three tiers plus the pledge, and the six scheme components. |
| `Statistic` | Portal counters, optionally refreshed from the transactional LEAN database. |
| `LoginPortal` | The stakeholder tiles that deep-link into the existing LEAN application. |
| `AuditLog` | Immutable record of every administrative write. |

Content entities are soft-deleted and carry created/updated audit columns,
applied automatically by the `DbContext`.

### The public route resolution

Concrete Angular routes win; anything left over is resolved against the CMS page
tree by slug. Publishing a page at a new address therefore needs no front-end
release.

---

## Security

- **Authentication** — JWT access tokens with refresh, held in `sessionStorage`
  so closing the tab ends the session. Refresh tokens are revoked on password
  change, deactivation and sign-out.
- **Authorisation** — five roles (`SuperAdmin`, `Administrator`, `Editor`,
  `Publisher`, `Viewer`) enforced by policy on the API and mirrored in the
  console. Editors write, publishers publish and delete, administrators manage
  users and settings.
- **Content sanitisation** — every rich-text field is sanitised server-side on
  write against an allow-list. Only YouTube, Vimeo and Google Maps embeds
  survive.
- **Uploads** — extension and content-type are checked against an allow-list,
  size is capped per type, the resolved path is verified to stay inside the
  uploads root, and files are served with `nosniff` and an explicit content-type
  map so an upload can never execute.
- **Rate limiting** — separate buckets for public reads, form submissions and
  sign-in, the last of which resists credential stuffing alongside Identity
  lockout.
- **Headers** — the GIGW-expected set is applied both by the API and by IIS,
  with a Content Security Policy that carries no `unsafe-inline` for scripts.
- **Audit** — every administrative write is recorded with actor, action, entity
  and IP address.

Secrets are never committed. `appsettings.Production.json` is git-ignored and
preserved across deployments; the connection string, JWT signing key and seed
password can all be supplied as environment variables instead.

---

## Accessibility

Built to the **Guidelines for Indian Government Websites (GIGW)** and **WCAG 2.1
Level AA**:

- Reader-controlled text scale (A- / A / A+) and a high-contrast theme, both
  persisted.
- Skip-to-main-content link, visible focus indicators, and focus moved to the
  main landmark on every route change so screen readers announce the new page.
- Semantic landmarks and heading structure; every table has a caption and scoped
  headers.
- Alternative text is a **required field** on banners and gallery images —
  the API rejects a save without it.
- The auto-scrolling ticker and both carousels can be paused, and stop
  automatically for `prefers-reduced-motion`.
- No colour-only signalling: status is always carried by text as well.

---

## Deployment

Provision once, then deploy repeatedly:

```powershell
# On the web server, elevated. Creates folders, app pools, site and /api.
.\deploy\scripts\Setup-IIS.ps1 -SiteName LeanPortal -SiteRoot C:\inetpub\LeanPortal -HostName lean.msme.gov.in

# On the database server, as sysadmin.
sqlcmd -S SQLSERVER01 -i .\database\scripts\01-create-database.sql

# Then, for every release.
.\deploy\scripts\Deploy-LeanPortal.ps1 -SiteRoot C:\inetpub\LeanPortal -AppPoolName LeanPortal -ApiAppPoolName LeanPortal-Api
```

The deployment script preserves the uploads folder and the production settings
file, stops and restarts the pools cleanly, reapplies write permissions and
health-checks the API.

Full instructions, including TLS, the transactional-system integration and the
backup schedule, are in [docs/deployment.md](docs/deployment.md).

---

## Further documentation

| Document | Covers |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Layering, request flow, key decisions and their trade-offs |
| [docs/deployment.md](docs/deployment.md) | Windows Server setup, IIS, SQL Server, TLS, backups, troubleshooting |
| [docs/cms-guide.md](docs/cms-guide.md) | Editor guide: publishing, the block builder, menus, media |
| [docs/api.md](docs/api.md) | Endpoint reference for the public and admin APIs |

---

## A note on the content

The seeded content is derived from the material published on
[lean.msme.gov.in](https://lean.msme.gov.in) so that the portal is usable and
reviewable from the first run. **It must be verified against the current scheme
guidelines by the Ministry before go-live** — figures, subsidy percentages and
scheme terms change.

The brand marks are the official artwork supplied by the client: the Ministry of
MSME lockup (which carries the State Emblem, the Hindi wordmark and the English
ministry name) and the MCLS scheme mark. Both are wired through site settings, so
they can be swapped from the CMS without a release. Use of the State Emblem is
governed by the State Emblem of India (Prohibition of Improper Use) Act, 2005 —
do not alter the artwork.

The remaining placeholder assets are the photographic ones: hero backgrounds,
gallery images and the partner logo tiles are abstract stand-ins, to be replaced
with real photography before launch.
