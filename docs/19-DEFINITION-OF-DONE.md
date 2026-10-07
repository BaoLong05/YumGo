# 19. Definition of Done

## 19.1 Code

- Compiles without warnings that indicate real correctness problems.
- Follows architecture dependency rules.
- Uses nullable reference types deliberately.
- No controller contains core business rules.
- No direct persistence access from controller methods.

## 19.2 Business behavior

- Happy path works.
- Invalid input is rejected.
- Invalid state transition is rejected.
- Authorization is enforced server-side.
- Resource scope is enforced.
- Concurrency behavior is defined.
- Idempotency is implemented where required.

## 19.3 Database

- Migration exists.
- Constraints and indexes are explicit.
- Query shape is understood.
- N+1 is avoided.
- Transactions cover required atomic changes.

## 19.4 API

- Request/response contract documented.
- HTTP status semantics consistent.
- ProblemDetails-style errors returned.
- Pagination rules documented.
- No sensitive data leaks.

## 19.5 Async/background

- CancellationToken flows through meaningful I/O.
- No `.Result`/`.Wait()` in request handling.
- Background consumers are retry-safe.
- Duplicate message processing is safe.

## 19.6 Testing

- Unit tests cover business invariants.
- Integration tests cover critical workflow.
- Concurrency/idempotency tests exist for critical write operations.

## 19.7 Observability

- Structured logs added.
- Correlation ID preserved.
- Relevant metrics/traces exist.
- Health behavior verified.

## 19.8 Documentation

- Feature behavior documented.
- API examples updated.
- State transitions updated if changed.
- ADR created for significant architectural decisions.

## 19.9 Portfolio standard

The feature is portfolio-ready when the developer can explain:
- why the rule lives where it does
- how the transaction works
- how the API is authorized
- how retries behave
- how failures are observed and debugged
