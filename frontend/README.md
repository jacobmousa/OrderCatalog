# Frontend Micro-Frontend Workspace

This directory hosts the React micro-frontend architecture for the ECommerce demo.

## Packages

- `web-shell` – Host container application (port 3000) with routing, dynamically loading remotes.
- `catalog-remote` – Exposes `ProductList` component via Module Federation (port 3001).

## Tech Stack

- Vite 5 + React 18 + TypeScript
- Module Federation via `@module-federation/vite`
- Shared singletons: `react`, `react-dom`

## Running Locally

From `frontend/` root (uses workspaces):

```bash
npm install
npm run dev --workspace=catalog-remote
npm run dev --workspace=web-shell
```

Then open http://localhost:3000 and navigate to Catalog.

## Environment Variables

`catalog-remote` uses `VITE_CATALOG_API_URL` (defaults to `http://localhost:5000`). Create a `.env` file inside `catalog-remote/` to override:

```
VITE_CATALOG_API_URL=http://localhost:5000
```

## Notes

- `ProductList` attempts to handle both array response or paged `{ items: [] }` shape.
- For a production build you would want a composed reverse proxy or HTML injection of remote URLs.
- Future: add `orders-remote` to expose order creation/confirmation flows.

## Testing (Planned)

Will add Jest + React Testing Library to each package (not yet included in scaffold).
