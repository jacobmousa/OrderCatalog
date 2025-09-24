# Assignment Coverage Matrix

This document maps each requirement to its implementation status with brief notes.

Legend: Implemented | Partial | Not Implemented | N/A (guidance)

## Assignment Guidance
- You have up to 3 days to work on it — N/A (guidance)
- Not all requirements are mandatory; prioritize — N/A (guidance)
- For any requirement not implemented, add a short note in README — Implemented (this file + README notes)
- Focus on delivering a working core system first — Implemented (core in place; extras optional)
  - Catalog service with product CRUD — Implemented (create, get, list+search, patch)
  - Orders service with draft → confirm/cancel — Implemented
  - React micro-frontend with at least one page — Implemented (Catalog and Orders pages)
  - Docker Compose running the stack — Partial (APIs + DB via compose; web via dev servers)
- Everything else (tests, OpenAPI, CI/CD, background worker, Kafka, Kubernetes, Playwright) is a bonus — N/A (guidance)

## Goal
- Build a minimal, realistic system with: — Implemented
  - Service A - Catalog API (.NET 8): manages products — Implemented
  - Service B - Orders API (.NET 8): manages orders & uses Catalog data — Implemented
  - React App - Micro-frontend (Vite MF): customer UI — Implemented
  - Dockerized: docker compose up brings everything up — Partial (APIs + DB via compose; web via dev servers)

## Functional Scope
### Entities
- Product: id, sku (unique), name, price, stock — Implemented
- Order: id, customerId, status (Draft, Confirmed, Cancelled), items { productId, sku, qty, unitPrice }, totalAmount — Implemented

### API Contracts
- Catalog API — Implemented
  - POST /api/products (create) — Implemented
  - GET /api/products?search=&page=&pageSize= — Implemented (server-side paging + search)
  - GET /api/products/:id — Implemented
  - PATCH /api/products/:id (price/stock updates) — Implemented
  - GET /health/* — Implemented
- Orders API — Implemented
  - POST /api/orders (create draft) — Implemented
  - POST /api/orders/:id/items (add item by productId or sku) — Implemented
  - POST /api/orders/:id/confirm — Implemented
  - POST /api/orders/:id/cancel — Implemented
  - GET /api/orders/:id — Implemented
  - GET /api/orders?status=&customerId=&page=&pageSize= — Implemented
  - GET /health/* — Implemented
- Cross-service rule: validate product via Catalog, copy sku & unitPrice (snapshot) — Implemented
- Stock checks are optional — Not Implemented (can validate qty <= stock; see notes)

## React Micro-Frontend (MF)
- Build a shell and one remote (or two) — Implemented (shell + catalog-remote + orders-remote)
  - Shell hosts layout, routing, shared deps — Implemented
  - Remote exposes at least one page — Implemented (CatalogPage and OrdersPage)
- Module Federation (Webpack 5) or Vite MF plugin — Implemented (Vite MF)
- Features — Partial
  - List products, search by name/sku — Implemented
  - Create an order: add/remove items — Partial (add implemented; remove not implemented)
  - Confirm/cancel order — Implemented
  - Show computed total — Implemented (server-computed; UI displays)
  - Optional: real-time toast/updates — Not Implemented

## Non-Functional Requirements
- Docker: docker-compose.yml starts DB(s), Catalog, Orders, and Web — Partial (DB + APIs; web via dev servers)
- DB: MS SQL with EF Core — Implemented
- Migrations: checked into repo — Implemented (including corrective rename for Orders)
- Validation: proper HTTP codes + Problem Details errors — Implemented
- Observability: structured logs; health checks; correlation id (x-correlation-id) — Implemented
- Testing (Bonus) — Partial
  - Backends: unit tests for domain; at least one integration test — Partial (present; can expand)
  - Frontend: 1-2 component tests or Playwright smoke — Not Implemented (planned)
- Security: no auth; design with it in mind — Implemented (no secrets logged; clean separation)
- Docs: clear README with run commands & decisions — Implemented (can be expanded further)

## Architecture Constraints & Guidance
- Services communicate over HTTP (REST) — Implemented
- Schema ownership per service; avoid shared DB — Implemented
- Avoid tight coupling: Orders must not reach into Catalog DB directly — Implemented (uses HttpClient)
- Bonus: publish OrderConfirmed event — Not Implemented (planned approach noted below)

## Example Scenarios to Support
1) Create products in Catalog → list them in the UI/API — Implemented
2) Create draft order → add items by sku/product → see total — Implemented
3) Confirm order → status changes to Confirmed — Implemented
4) Cancel order → status to Cancelled — Implemented
5) Update a product price in Catalog → new orders use new price; existing orders keep snapshot — Implemented

## Additional Bonuses
- OpenAPI (Swagger) published by both services — Implemented
- Typed SDK generation for the UI — Not Implemented (would use NSwag/OpenAPI TS)
- Background worker to auto-cancel stale Draft orders — Not Implemented (HostedService scanning)
- Message bus (Kafka/Rabbit/Redis) emitting OrderConfirmed — Not Implemented (outbox + publisher)
- CI YAML (build, test, docker build) — Partial (build/test enabled; docker build/deploy template prepared but disabled)
- K8s manifests (Deployment/Service) — Not Implemented
- Playwright E2E smoke against docker compose — Not Implemented (planned)

## Notes on Non-Implemented Items
- Stock checks: validate requested qty <= stock in Catalog; optionally reserve stock on confirm (two-phase or eventual consistency).
- Typed SDKs: generate C# and TS clients via NSwag in CI; publish NPM package for frontend.
- Background worker: add IHostedService in Orders to cancel stale Draft orders older than X minutes.
- Messaging: use an outbox pattern in Orders; publish `OrderConfirmed` to Kafka/Rabbit; consume downstream.
- K8s: add manifests/Helm chart; use kustomize overlays per environment.
- Playwright: run against `docker compose up` in CI after waiting for health endpoints.
