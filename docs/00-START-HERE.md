# YumGo — Complete Documentation

This folder is the canonical documentation set for the YumGo project.

## 1. Reading order

Do not start with feature code.

```text
01-BUSINESS
    ↓
02-DATABASE
    ↓
03-ARCHITECTURE
    ↓
04-API
    ↓
05-SECURITY
    ↓
06-FLOWS
    ↓
07-REALTIME-PAYMENT-JOBS
    ↓
08-TESTING-VALIDATION
    ↓
09-OPERATIONS
    ↓
10-IMPLEMENTATION
    ↓
11-DECISIONS
```

## 2. Non-negotiable project rules

### Database first

PostgreSQL is the canonical database. Database schema, relationships, constraints and indexes are established before implementing business features.

### IDs

All business/resource identifiers use NanoID with a fixed length of 21 characters.

This applies to:
- Primary keys
- Foreign keys
- URL resource IDs
- Form IDs / hidden IDs
- API request identifiers
- Resource references

Do not use integer auto-increment IDs, BIGSERIAL, IDENTITY, UUID or GUID as business/resource identifiers.

NanoID is not authorization. Every resource ID supplied by a client must be validated for existence, ownership/scope and permission.

### Hidden fields

Hidden fields are transport conveniences only. A hidden `branchId`, `menuItemId`, `addressId`, etc. must always be treated as untrusted input and revalidated on the server.

### Validation coverage

Validation is required at three levels:

```text
Request / DTO
    ↓
Application + Domain
    ↓
PostgreSQL constraints
```

Cross-field, cross-table, ownership and state-machine rules must be covered by tests.

### Money

Monetary values use PostgreSQL `numeric`, not floating point.

### Time

Persist timestamps as `timestamptz` in UTC. Business timezone must be explicit where a rule depends on local time.

### Soft delete

Where soft delete is specified, queries must exclude deleted rows by default. Unique indexes for soft-deleted tables use partial indexes where appropriate.

## 3. Canonical precedence

When two older documents conflict:

1. `02-DATABASE` wins for PostgreSQL schema, IDs, constraints and relationships.
2. `05-SECURITY` wins for authentication, authorization and resource scope.
3. `01-BUSINESS` wins for business behavior and actor rules.
4. `04-API` wins for HTTP/API contract.
5. `08-TESTING-VALIDATION` wins for validation and negative-case coverage.
6. `10-IMPLEMENTATION` wins for build sequence.
7. `11-DECISIONS` records why a technical choice exists.

Legacy reference material is retained only under `12-REFERENCE` and is not authoritative where it conflicts with the canonical rules above.

## 4. Required implementation order

```text
Phase 0  Documentation and repository bootstrap
Phase 1  PostgreSQL schema
Phase 2  Constraints / indexes / seed reference data
Phase 3  Database integrity tests
Phase 4  Authentication foundation
Phase 5  Customer identity/profile/address
Phase 6  Restaurant/branch/menu
Phase 7  Discovery
Phase 8  Cart / promotion / checkout
Phase 9  Place order
Phase 10 Restaurant order processing
Phase 11 Driver / assignment / delivery
Phase 12 Payment / webhook / refund
Phase 13 Realtime tracking / notification
Phase 14 Reviews / operations / admin
Phase 15 Observability / performance / deployment hardening
```

A later feature must not silently redesign an earlier database contract. Changes go through the migration and ADR process.
