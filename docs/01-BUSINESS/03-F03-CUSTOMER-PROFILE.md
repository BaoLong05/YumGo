# F03 — Customer Profile

**Actor:** Customer


### Actions
Read/update own profile.

### Validation
- fullName 2..120;
- dateOfBirth not future;
- avatar URL allowlisted protocols;
- `userId` must come from auth context, not client ownership field.

### DB
Update only the current user's `customer_profiles` row and related `users.phone` when that field is explicitly allowed.


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
