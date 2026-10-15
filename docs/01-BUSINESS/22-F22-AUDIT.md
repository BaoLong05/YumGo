# F22 — Audit

**Actor:** System / Admin


Every security-sensitive mutation records actor, resource, action, timestamp and relevant before/after values.

Do not log passwords, access/refresh tokens, payment secrets or unnecessary sensitive personal data.

Audit rows are append-only from application perspective.


## Common security rule
All resource identifiers use NanoID(21). Route IDs and hidden form IDs are never trusted by themselves; the backend must re-check authorization and record relationships before mutation or response.

## Definition of Done
- DTO validation implemented.
- Cross-field validation implemented.
- Cross-table validation implemented.
- Authorization/scope validation implemented.
- Database constraints and indexes reviewed.
- Transaction boundary reviewed.
- Idempotency/concurrency behavior defined where applicable.
- Audit/notification side effects defined.
- Integration tests cover happy path and negative cases.
