# YumGo Documentation Manifest

| Area | Canonical location | Purpose |
|---|---|---|
| Business | `01-BUSINESS/` | Actor workflows and business rules |
| Database | `02-DATABASE/` | PostgreSQL schema, fields, relationships, constraints, indexes |
| Architecture | `03-ARCHITECTURE/` | Clean Architecture, DDD, modules, actors |
| API | `04-API/` | HTTP contract and ID conventions |
| Security | `05-SECURITY/` | NanoID security, authentication, authorization |
| Flows | `06-FLOWS/` | Order flows and state machines |
| Realtime/Payment/Jobs | `07-REALTIME-PAYMENT-JOBS/` | SignalR, payments, notifications, jobs |
| Testing/Validation | `08-TESTING-VALIDATION/` | Test strategy and validation coverage |
| Operations | `09-OPERATIONS/` | Cache, observability, deployment, UI route reference |
| Implementation | `10-IMPLEMENTATION/` | Migration, build sequence, checklist, DoD |
| Decisions | `11-DECISIONS/` | Architecture decision records |
| Reference | `12-REFERENCE/` | Legacy/base documents kept for traceability |

## Mandatory cross-checks before coding a feature

- Business rules exist.
- Required DB fields exist.
- PK/FK relationships are defined.
- Unique/check constraints are defined where possible.
- Cross-field validation is documented.
- Cross-table validation is documented.
- Ownership/resource-scope rules are documented.
- State transitions are documented.
- API request/response fields are documented.
- Negative tests exist.
- Idempotency requirements are defined for retry-sensitive commands.
