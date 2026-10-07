# F09 — Promotions

**Actor:** Customer / Restaurant / Admin


### Validation
- code normalized for comparison;
- now between starts_at and ends_at;
- restaurant scope matches order branch when promotion is restaurant-specific;
- usage limit not exceeded;
- per-user limit not exceeded;
- minimum order amount met;
- percentage discount <= max discount when configured;
- free delivery cannot exceed eligible delivery fee.

Promotion is revalidated again during Place Order; UI preview is not authoritative.


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
