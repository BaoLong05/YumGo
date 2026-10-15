# F04 — Addresses

**Actor:** Customer


### Actions
Create, edit, delete, set default, list own addresses.

### Validation
- recipient name/phone/address parts required;
- coordinates both-null or both-present;
- coordinates inside geographic ranges;
- only one default address per user;
- address ID in URL must belong to authenticated user.

### Hidden field
Forms may carry `addressId` as hidden NanoID, but server must re-check ownership.


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
