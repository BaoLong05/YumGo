# F18 — Reviews

**Actor:** Customer


### Preconditions
- order belongs to customer;
- order delivered;
- no existing review for that order/customer.

### Validation
- rating 1..5;
- comment within max length;
- review restaurant comes from order, not client-provided unrelated restaurant ID.


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
