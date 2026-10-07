# 13. Testing Strategy

## 13.1 Test projects

```text
YumGo.Domain.Tests
YumGo.Application.Tests
YumGo.Infrastructure.Tests
YumGo.Api.IntegrationTests
```

## 13.2 Domain unit tests

High priority:
- order state transitions
- cancellation eligibility
- total calculations
- money arithmetic
- cart branch consistency
- promotion rules
- delivery transitions
- driver eligibility rules

Domain tests must not require a database.

## 13.3 Application tests

Test:
- authorization outcomes
- use-case orchestration
- repository calls
- idempotency behavior
- transaction decisions
- external provider failures
- event creation

## 13.4 Integration tests

Use a real PostgreSQL-compatible test database and the actual EF Core mappings.

High-value flows:
- register/login/refresh
- place order
- restaurant accept/order prepare
- payment webhook
- driver accept/pickup/complete
- resource-scope authorization
- pagination/filtering
- outbox persistence

## 13.5 API host tests

Use `WebApplicationFactory` to test:
- middleware
- authentication
- authorization
- JSON contracts
- status codes
- database integration

## 13.6 Contract tests

Client-facing request/response examples in `21-CONTRACT-EXAMPLES.md` are treated as contract fixtures. Breaking changes require an explicit API versioning decision.

## 13.7 Important concurrency tests

Test at least:

1. Two identical order requests with the same idempotency key create one order.
2. Two drivers cannot both accept the same delivery.
3. Duplicate payment webhooks do not duplicate state changes.
4. A stale order version cannot overwrite a newer state transition.

## 13.8 Coverage

Coverage is a signal, not the objective. The objective is protection of business invariants and critical workflows.

Minimum quality bar:
- all aggregate invariants covered
- all critical state transitions covered
- all security-sensitive use cases covered
- all critical integration paths covered
