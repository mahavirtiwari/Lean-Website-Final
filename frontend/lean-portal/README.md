# LEAN Portal — Angular front end

Public site and admin CMS console for the MSME Competitive (LEAN) Scheme portal.

See the [repository README](../../README.md) for the full picture, and
[docs/architecture.md](../../docs/architecture.md) for how this app is put
together.

## Commands

```bash
npm install          # restore
npm start            # dev server on http://localhost:4200
npm run build        # production build into dist/lean-portal/browser
npm test             # unit tests (vitest)
```

The dev server expects the API on `http://localhost:5199`; see
`src/environments/environment.ts`. In production both are served from the same
origin, so `apiUrl` is the relative path `/api`.

## Where things live

| Path | Contents |
|---|---|
| `src/styles/_tokens.scss` | Design tokens. A rebrand is this one file. |
| `src/app/core/` | Models, services, HTTP interceptors, route guards |
| `src/app/shared/` | Inline SVG icon set, pipes, shared components |
| `src/app/layout/` | Public and admin shells |
| `src/app/features/public/` | Public screens, including the home page bands |
| `src/app/features/admin/` | CMS screens |
