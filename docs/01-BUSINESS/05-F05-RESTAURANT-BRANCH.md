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
- Staff assignment and staff-scoped management are not part of this first slice; they require the restaurant-staff authorization workflow.

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
