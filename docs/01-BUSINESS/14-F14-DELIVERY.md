# F14 — Delivery

**Actor:** Driver / Customer / Operations


### Rules
- only assigned driver can mark picked_up/delivering/completed;
- pickup completion requires order ready according to business flow;
- delivered transition requires valid delivery state;
- customer cannot self-mark delivered;
- failed delivery requires reason;
- cancellation after pickup follows explicit policy and may require operations action.


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
