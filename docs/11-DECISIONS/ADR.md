# 20. Architecture Decision Records

## ADR-001 — Modular monolith first

### Decision
Use a modular monolith as the first production architecture.

### Reason
The main goal is to demonstrate strong domain boundaries and production engineering without paying microservice operational complexity before it is needed.

### Consequence
Modules share infrastructure deployments but communicate through application contracts/events rather than direct access to another module's internals.

## ADR-002 — PostgreSQL is authoritative

### Decision
PostgreSQL is the source of truth for accounts, restaurants, menus, carts, orders, payments, deliveries, reviews, notifications and audit history.

### Reason
The core product is transaction-heavy and relational.

## ADR-003 — Redis for ephemeral/fast-changing state

### Decision
Redis stores current driver location, cache entries and rate-limit state.

### Reason
These data have high access frequency or short-lived semantics and should not force PostgreSQL to absorb every live update.

## ADR-004 — SignalR for realtime

### Decision
Use SignalR for server-pushed order and tracking updates.

### Reason
The backend is ASP.NET Core and requires authenticated server-to-client live updates.

## ADR-005 — Outbox for durable events

### Decision
Write integration/domain event facts to an outbox in the same database transaction as business state changes.

### Reason
Avoid losing an event between database commit and message publication.

## ADR-006 — Explicit DTOs

### Decision
Public API contracts use dedicated request/response DTOs.

### Reason
Prevents over-posting and decouples transport contracts from domain/persistence structure.

## ADR-007 — One mobile app

### Decision
Maintain one Expo application with role-driven customer/driver experiences.

### Reason
Avoid duplicating authentication, network, shared UI and build infrastructure.

## ADR-008 — No generic repository by default

### Decision
Use domain/application-specific repository ports where they add value; do not create one generic repository for every entity.

### Reason
A generic CRUD abstraction usually hides important query/use-case semantics and duplicates EF Core.

## ADR-009 — Cursor pagination for scale-sensitive collections

### Decision
Use cursor pagination for high-volume list endpoints.

### Reason
Stable performance and deterministic traversal at larger data volumes.

## ADR-010 — Provider adapters

### Decision
Payment/maps/notification providers are hidden behind application ports.

### Reason
Domain behavior must not depend on vendor SDKs and provider replacement should not require a domain rewrite.
