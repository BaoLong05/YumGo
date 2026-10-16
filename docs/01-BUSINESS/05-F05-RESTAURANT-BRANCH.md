# F05 — Restaurant / Branch

**Actor:** Restaurant Owner / Staff / Admin


### Rules
- restaurant owner may manage only their own restaurant;
- staff may manage only the restaurant granted by role assignment;
- branch must belong to the selected restaurant;
- branch name unique within restaurant;
- coordinates required;
- schedule fields must be both present or both absent;
- suspended restaurant cannot publish an open branch.


## Common security rule
All resource identifiers use NanoID(21). Route IDs and hidden form IDs are never trusted by themselves; the backend must re-check authorization and record relationships before mutation or response.

## Initial backend slice
- An authenticated owner can create a restaurant; the owner ID is taken from the access token, not the request body. New restaurants start as `pending`.
- An owner can list their restaurants and create/list branches only for a restaurant they own. Foreign or missing restaurants return the same not-found response.
- New branches start as `draft`; creating a branch does not publish it. Branch coordinates are required and bounded, and opening/closing times must be provided together. Overnight schedules are not supported in this slice; opening time must precede closing time.
- Restaurant creation and branch creation write an audit event in the same PostgreSQL transaction. They do not send notifications.
## Restaurant review and operations workflow
- Only an authenticated, active, email-verified platform `admin` may review a restaurant. Review is allowed only from `pending`, producing `active` on approval or `rejected` on rejection. Rejection requires a reason (1..500 characters). State and audit record commit atomically; repeated reviews conflict.
- An owner may publish an owned branch (`draft`/`closed` to `open`) or hide it (`draft`/`open` to `closed`) only while the restaurant is `active`. A suspended branch cannot be changed by its owner. Foreign resources return `404`; invalid state transitions return `409`. Identical visibility updates are idempotent and do not create duplicate audit rows.
- An owner replaces the full restaurant-role set for an active, email-verified employee using `roles[]`. Allowed values are `manager` and `staff`; an empty array deactivates all role assignments. Role changes are transactional and audited. The API deliberately accepts a complete set, so a future permission screen can map each checkbox selection directly to the array; the client does not decide authority or scope.
- Restaurant staff assignments do not grant global platform roles. Any later staff access to restaurant/branch operations must independently enforce the active assignment and resource scope.

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
- PostgreSQL integration tests cover review, visibility, role replacement, ownership, authorization, and audit outcomes.
