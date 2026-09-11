# API reference

Base URL: `/api`. Swagger UI is available at `/swagger` in Development.

Enums serialise as strings. Errors use RFC 7807 problem details with a
`traceId`. All timestamps are ISO 8601 with an offset.

---

## Public endpoints

Anonymous. Rate limited to 120 requests per minute per client (form endpoints to
5). Responses are output-cached for five minutes.

### Site

| Method | Path | Returns |
|---|---|---|
| `GET` | `/api/site/settings` | Public site settings as a key/value map |
| `GET` | `/api/site/navigation` | All five menus, each as a nested tree |
| `GET` | `/api/site/sitemap` | Hierarchical index of published pages |
| `GET` | `/sitemap.xml` | XML sitemap for crawlers |
| `GET` | `/health` | Liveness probe |

### Pages

| Method | Path | Notes |
|---|---|---|
| `GET` | `/api/pages/{**slug}` | Hierarchical slug, e.g. `about-scheme/objective`. Returns the page with its breadcrumbs, sibling navigation and blocks. |
| `GET` | `/api/pages/{id}/children` | Published children of a page |

### Home

| Method | Path | Notes |
|---|---|---|
| `GET` | `/api/home` | The whole landing page in one response — page and blocks, banners, statistics, components, levels, portals, featured documents, latest and ticker posts, testimonials, useful links |

### Content

| Method | Path | Query |
|---|---|---|
| `GET` | `/api/posts` | `page`, `pageSize`, `search`, `type` |
| `GET` | `/api/posts/{slug}` | Returns the post with related items; increments the view count |
| `GET` | `/api/documents` | `category`, `search` |
| `GET` | `/api/documents/{id}/download` | Records the download, then 302s to the file |
| `GET` | `/api/faqs` | `search`. Grouped by category. |
| `GET` | `/api/gallery` | Album summaries |
| `GET` | `/api/gallery/{slug}` | Album with images |
| `GET` | `/api/programmes` | `page`, `pageSize`, `search`, `state`, `district`, `type`, `agency`, `status` |
| `GET` | `/api/programmes/filters` | Distinct filter values; pass `state` to narrow the districts |

### Scheme reference data

| Method | Path |
|---|---|
| `GET` | `/api/scheme/levels` |
| `GET` | `/api/scheme/components` |
| `GET` | `/api/scheme/statistics` |
| `GET` | `/api/scheme/login-portals` |
| `GET` | `/api/scheme/partners` (`type`) |
| `GET` | `/api/scheme/testimonials` |

### Forms

| Method | Path | Notes |
|---|---|---|
| `POST` | `/api/contact` | Returns `202` with a reference number, e.g. `LEAN-ENQ-000042` |
| `POST` | `/api/subscribe` | Newsletter sign-up; idempotent for an address already present |

Both carry a honeypot field named `website`. A request with it filled is
accepted and silently discarded.

---

## Authentication

| Method | Path | Notes |
|---|---|---|
| `POST` | `/api/auth/login` | Returns an access token, a refresh token and the current user. Rate limited to 8 per minute. |
| `POST` | `/api/auth/refresh` | Exchanges a refresh token for a new pair |
| `GET` | `/api/auth/me` | The signed-in user |
| `POST` | `/api/auth/change-password` | Revokes the refresh token, so the caller must sign in again |
| `POST` | `/api/auth/logout` | Revokes the refresh token |

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "admin@lean.msme.gov.in", "password": "…" }
```

```json
{
  "accessToken": "eyJhbGciOi…",
  "refreshToken": "…",
  "expiresAt": "2026-09-04T13:00:00+00:00",
  "user": {
    "id": "…", "email": "admin@lean.msme.gov.in", "fullName": "Portal Administrator",
    "mustChangePassword": false, "roles": ["SuperAdmin"]
  }
}
```

Send the access token as `Authorization: Bearer <token>`.

A failed sign-in always returns the same message regardless of cause, so the
endpoint cannot be used to discover which addresses have accounts. Repeated
failures lock the account for 15 minutes (`423 Locked`).

When `mustChangePassword` is true, the client must send the operator to the
password-change screen before anything else.

---

## Admin endpoints

All require a bearer token. Authorisation is by policy:

| Policy | Roles | Applies to |
|---|---|---|
| `CanEdit` | SuperAdmin, Administrator, Editor, Publisher | Create and update |
| `CanPublish` | SuperAdmin, Administrator, Publisher | Publish, unpublish, delete |
| `CanAdminister` | SuperAdmin, Administrator | Users, settings, activity log |

### Dashboard

| Method | Path |
|---|---|
| `GET` | `/api/admin/dashboard` |
| `GET` | `/api/admin/dashboard/activity` (`page`, `pageSize`, `entityName`) |

### Pages and blocks

| Method | Path | Notes |
|---|---|---|
| `GET` | `/api/admin/pages` | `page`, `pageSize`, `search`, `status`, `parentId` |
| `GET` | `/api/admin/pages/tree` | Flat list for the parent picker |
| `GET` `POST` | `/api/admin/pages` | |
| `GET` `PUT` `DELETE` | `/api/admin/pages/{id}` | Delete is blocked by children or menu references (409) |
| `POST` | `/api/admin/pages/{id}/status` | `{ "status": "Published" }` |
| `POST` | `/api/admin/pages/reorder` | |
| `GET` `POST` | `/api/admin/pages/{pageId}/blocks` | |
| `PUT` `DELETE` | `/api/admin/pages/{pageId}/blocks/{blockId}` | |
| `POST` | `/api/admin/pages/{pageId}/blocks/reorder` | |

### Posts

`GET` `POST` `/api/admin/posts`, `GET` `PUT` `DELETE` `/api/admin/posts/{id}`,
`POST` `/api/admin/posts/{id}/status`.

### Uniform resources

These ten share an identical contract:

```
GET    /api/admin/{resource}            page, pageSize, search
GET    /api/admin/{resource}/{id}
POST   /api/admin/{resource}
PUT    /api/admin/{resource}/{id}
DELETE /api/admin/{resource}/{id}       CanPublish
POST   /api/admin/{resource}/reorder
```

`banners` · `faqs` · `documents` · `statistics` · `login-portals` ·
`scheme-levels` · `scheme-components` · `testimonials` · `partners` ·
`programmes`

These return **admin projections** which include the editorial fields the public
DTOs omit — `sortOrder`, `isActive`, `isFeatured`, scheduling. Send the full
object back on update: the request contracts are complete replacements, not
patches.

### Navigation, gallery, media

| Method | Path |
|---|---|
| `GET` `POST` | `/api/admin/menu` (`location`) |
| `GET` `PUT` `DELETE` | `/api/admin/menu/{id}` |
| `POST` | `/api/admin/menu/reorder` |
| `GET` `POST` | `/api/admin/gallery` |
| `GET` `PUT` `DELETE` | `/api/admin/gallery/{id}` |
| `POST` | `/api/admin/gallery/{albumId}/images` |
| `PUT` `DELETE` | `/api/admin/gallery/{albumId}/images/{imageId}` |
| `GET` | `/api/admin/media` (`page`, `pageSize`, `search`, `folder`) |
| `GET` | `/api/admin/media/folders` |
| `POST` | `/api/admin/media/upload` — multipart: `file`, `folder`, `altText`, `caption` |
| `PUT` `DELETE` | `/api/admin/media/{id}` |

Uploads are checked against an extension and content-type allow-list, capped at
5 MB for images and 25 MB for documents, and the resolved path is verified to
stay inside the uploads root.

### Enquiries, settings, users

| Method | Path | Policy |
|---|---|---|
| `GET` | `/api/admin/enquiries` (`page`, `pageSize`, `search`, `status`, `category`) | CanEdit |
| `GET` `PUT` | `/api/admin/enquiries/{id}` | CanEdit |
| `DELETE` | `/api/admin/enquiries/{id}` | CanAdminister |
| `GET` | `/api/admin/enquiries/export` — CSV | CanEdit |
| `GET` `PUT` | `/api/admin/settings` (`group`) | CanAdminister |
| `GET` | `/api/admin/settings/groups` | CanAdminister |
| `GET` `POST` | `/api/admin/users` | CanAdminister |
| `GET` `PUT` `DELETE` | `/api/admin/users/{id}` | CanAdminister |
| `POST` | `/api/admin/users/{id}/reset-password` | CanAdminister |
| `GET` | `/api/admin/users/roles` | CanAdminister |

`PUT /api/admin/settings` rejects unknown keys rather than creating them, so a
typo cannot silently add a setting nothing reads. `DELETE` on a user deactivates
rather than removes, preserving the audit trail.

---

## Conventions

### Paged responses

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 57,
  "totalPages": 3,
  "hasPrevious": false,
  "hasNext": true
}
```

`pageSize` is capped at 100.

### Errors

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "The slug 'about-scheme' is already in use by another page.",
  "status": 400,
  "instance": "/api/admin/pages",
  "traceId": "0HN7…"
}
```

| Status | Meaning |
|---|---|
| `400` | Validation failed; `title` carries the message |
| `401` | Missing, invalid or expired token |
| `403` | Authenticated but the role does not permit the action |
| `404` | Not found, or not published |
| `409` | Blocked by a reference — a page with children, a menu item with sub-items |
| `423` | Account locked after repeated failed sign-ins |
| `429` | Rate limit exceeded |
| `500` | Unhandled; details are logged against the `traceId`, never returned |

### Reorder

```json
{ "items": [ { "id": 3, "sortOrder": 1 }, { "id": 1, "sortOrder": 2 } ] }
```

Send the complete ordered list for the affected set.
