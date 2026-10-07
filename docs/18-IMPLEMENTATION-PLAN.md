# 18. Implementation Plan

## Phase 0 — Repository bootstrap

Done before business features:
- monorepo
- backend projects
- web/mobile projects
- Docker local infrastructure
- contracts package
- docs
- CI skeleton

Exit criteria: build/test/typecheck/doctor checks pass.

## Phase 1 — Identity and access

Deliver:
- register
- login
- refresh
- logout
- profile
- role assignment
- authorization policies
- password security

Tests:
- invalid credentials
- suspended user
- refresh rotation
- ownership policy

## Phase 2 — Restaurant and menu

Deliver:
- restaurant/branch management
- menu/category/item CRUD
- availability/sold-out state
- public restaurant discovery
- menu caching

## Phase 3 — Cart and promotions

Deliver:
- active cart per branch
- quantity updates
- promotion validation
- price revalidation
- expiration

## Phase 4 — Order and payment

Deliver:
- order placement
- COD
- online payment adapter
- webhook processing
- cancellation/refund policy
- idempotency
- outbox events

## Phase 5 — Driver/delivery

Deliver:
- driver availability
- assignment
- acceptance/rejection
- pickup
- completion/failure
- delivery history

## Phase 6 — Realtime tracking

Deliver:
- SignalR hub
- authenticated groups
- current location in Redis
- sampled location history
- customer tracking screen
- driver location ingestion

## Phase 7 — Notifications/background

Deliver:
- in-app notifications
- push adapter
- outbox publisher
- durable broker consumers
- retries/dead-letter handling
- reconciliation jobs

## Phase 8 — Review and operations

Deliver:
- reviews
- operational dashboard
- audit events
- payment reconciliation views
- delivery monitoring

## Phase 9 — Hardening

Deliver:
- rate limiting
- security review
- query tuning
- load testing of critical endpoints
- observability dashboards
- deployment/rollback verification

## Feature implementation checklist

For every feature:

```text
Requirement
 ↓
Use case
 ↓
Business rules
 ↓
Domain model
 ↓
Application contract
 ↓
Infrastructure adapter
 ↓
API endpoint
 ↓
Client contract
 ↓
Unit tests
 ↓
Integration tests
 ↓
Observability
 ↓
Documentation update
```
