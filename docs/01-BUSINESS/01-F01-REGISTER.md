# F01 — Register

**Actor:** Customer


### Goal
Create a new customer account safely.

### Inputs
- email: required, 5..254, lowercase-normalized, syntactically valid
- password: required, at least 6 characters and at least 3 of 4 character groups (uppercase, lowercase, digit, symbol)
- fullName: required, 2..120 after trim
- phone: optional, normalized and unique when present

### Validation
- email must be unique across persisted users under the current database constraint;
- phone must be unique across persisted users when present under the current database constraint;
- password is hashed server-side;
- no client-provided user ID/status/role accepted;
- role is assigned server-side as Customer;
- profile, user and the server-assigned Customer role are created atomically.

### DB writes
`users` + `customer_profiles` + `user_roles`; no email-verification record is created by this slice.

### Failure cases
409 generic duplicate-account response (email/phone are not distinguished); 422 invalid fields; 429 registration rate limit; generic 500 for unexpected failure.

### Acceptance
A successful `POST /api/v1/auth/register` creates exactly one user, customer profile and Customer role link; a repeated request cannot create duplicates. The response contains only the public profile and does not issue access/refresh tokens.


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
