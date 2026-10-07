# F12 — Restaurant Order Processing

**Actor:** Restaurant Staff


### Actions
Confirm, reject where policy allows, start preparation, mark ready.

### Validation
- actor must have restaurant/branch scope;
- order must belong to actor's branch;
- transition must be legal by order state machine;
- branch must be active/suspended rules must be checked.

### Audit
Every state change records actor, old state, new state, timestamp.


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
