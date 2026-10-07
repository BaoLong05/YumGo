# Backend Architecture

**Status:** `<...>`

## 1. Architecture style

`<Modular monolith / Clean Architecture / services / other>`

## 2. Layers / modules

```text
Presentation
    |
    v
Application
    |
    v
Domain
    ^
    |
Infrastructure implements ports
```

## 3. Domain

- Entities
- Value Objects
- Aggregates
- Domain Services
- Domain Events
- Domain Exceptions

## 4. Application

Describe use cases, commands, queries, DTOs, policies, and ports.

## 5. Infrastructure

Describe database, cache, storage, messaging, external providers, and adapters.

## 6. Cross-cutting concerns

Authentication, authorization, validation, error handling, logging, tracing, configuration, idempotency, rate limiting.

## 7. Transaction boundaries

Document which operations are transactional and why.

## 8. Background processing

Document workers, queues, retry policies, and idempotency.

## 9. Module dependency rules

Document which modules may depend on which modules.

## 10. Anti-patterns prohibited in this project

`<e.g. controller-to-DB coupling, domain depending on infrastructure, etc.>`
