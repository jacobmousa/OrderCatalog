# ECommerce Demo (Catalog + Orders)

Monorepo with .NET 8 Web APIs (Catalog, Orders) and React micro-frontends (Vite + Module Federation).

## Demo

![OrderCatalog demo](./OrderCatalog.gif)

## Contents
- `src/Catalog.Api` – Products API (search, filter, CRUD subset)
- `src/Orders.Api` – Orders API (drafts, items, confirm/cancel)
- `frontend/` – Micro-frontends
  - `web-shell` – host shell (port 3000)
  - `catalog-remote` – Catalog UI (port 3001)
  - `orders-remote` – Orders UI (port 3002)
- `docker-compose.yml` – Dev DB (SQL Server) + APIs wiring

## Quick start

Option A: Docker (recommended for APIs + DB)

```powershell
# from repo root
docker compose up --build -d
# APIs available at http://localhost:5000 (Catalog) and http://localhost:5001 (Orders)
```

Option B: Run APIs locally (requires SQL Server at localhost,14333 or adjust connection strings)

```powershell
# Catalog
dotnet run --project .\src\Catalog.Api\Catalog.Api.csproj
# Orders
dotnet run --project .\src\Orders.Api\Orders.Api.csproj
```

Frontend dev servers

```powershell
# from frontend/
npm install
npm run dev --workspace=catalog-remote
npm run dev --workspace=orders-remote
npm run dev --workspace=web-shell
```

Visit `http://localhost:3000`.

## Configuration
- SQL Server SA password can be overridden via a `.env` file next to `docker-compose.yml`:

```
SA_PASSWORD=Stronger_Passw0rd!
```

- Frontend env vars (create `.env` in each package if needed):
  - `VITE_CATALOG_API_URL` (default `http://localhost:5000`)
  - `VITE_ORDERS_API_URL` (default `http://localhost:5001`)

## Development notes
- Orders API startup now relies solely on EF Core migrations (no dev-only DDL fallback).
- Explicit table mappings: `Orders`, `OrderItems`.
- Added migration to rename legacy `OrderItem` to `OrderItems` and ensure FK/index consistency.

## License
MIT
