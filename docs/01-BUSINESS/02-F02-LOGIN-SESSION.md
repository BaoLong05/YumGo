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

## Security
Refresh token is stored hashed. Reuse/revocation of rotated refresh tokens is rejected.

### Confirmed session lifetime
- The absolute session deadline is 48 hours after login; refresh does not extend it.
- Access JWTs are short-lived (15 minutes by default) and cannot outlive the session deadline.
- Every access JWT is bound to its current refresh-token row. Authenticated requests verify that row and the user account are still active, so logout, refresh rotation, expiry, or account suspension invalidates the prior access JWT immediately.
- Refresh tokens are opaque, random, single-use, and stored as SHA-256 hashes. A rotated token inherits the original session deadline; reuse revokes its descendant chain.
- Login success/failure, refresh rotation/reuse, and logout produce audit events without password or token values.
- Login is limited to 5 requests/minute per observed client IP; refresh is limited to 10 requests/minute per observed client IP (per API instance).

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
