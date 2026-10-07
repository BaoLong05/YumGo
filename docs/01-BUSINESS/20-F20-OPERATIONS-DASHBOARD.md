# F20 — Operations Dashboard

**Actor:** Operations / Admin


### Data
Dashboard reads orders, deliveries, drivers, payments, alerts and audit summaries.

### Validation
- role/policy required;
- query limits bounded;
- date range normalized and max span restricted;
- dashboard never exposes full secrets or unrelated customer private data.


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
