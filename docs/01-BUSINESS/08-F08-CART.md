# F08 — Cart

**Actor:** Customer


### Rules
- cart belongs to authenticated customer;
- cart has exactly one branch;
- cart item must belong to same branch;
- menu item must still be active/available at mutation time;
- quantity 1..99;
- unit price is server-read, never client-trusted;
- duplicate item behavior is deterministic.

### Concurrency
Two quantity updates on same item must not silently lose data. Use transaction/row-level strategy and return final quantity.


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
