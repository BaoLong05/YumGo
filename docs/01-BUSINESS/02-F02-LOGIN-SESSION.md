# F02 — Login / Session

**Actor:** Customer / Staff / Driver


### Inputs
email + password.

### Validation
- normalize email;
- find user by lower(email);
- user must not be disabled/suspended for login;
- password hash must verify;
- roles are loaded from DB, not accepted from client;
- create short-lived access token + hashed refresh token.

### Security
Refresh token is stored hashed. Reuse/revocation of rotated refresh tokens is rejected.

### Acceptance
Invalid credentials produce the same outward failure shape regardless of whether email exists, while audit logs may record the internal reason.


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
