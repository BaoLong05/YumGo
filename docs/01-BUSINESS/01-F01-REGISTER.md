# F01 — Register

**Actor:** Customer


### Goal
Create a new customer account safely.

### Inputs
- email: required, 5..254, lowercase-normalized, syntactically valid
- password: required, minimum policy length, must pass password strength policy
- fullName: required, 2..120 after trim
- phone: optional, normalized and unique when present

### Validation
- email must not belong to another non-deleted user;
- phone must not belong to another active user;
- password is hashed server-side;
- no client-provided user ID/status/role accepted;
- role is assigned server-side as Customer;
- profile and user are created atomically.

### DB writes
`users` + `customer_profiles`; optional verification record if email verification is enabled.

### Failure cases
409 duplicate email/phone; 422 invalid fields; generic 500 for unexpected failure.

### Acceptance
A successful registration creates exactly one user and one profile; a repeated request cannot create duplicates.


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
