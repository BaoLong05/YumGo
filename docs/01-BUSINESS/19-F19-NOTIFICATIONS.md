# F19 — Notifications

**Actor:** System / User


### Rules
- notification recipient comes from server event;
- user cannot create notifications for another user;
- device token belongs to authenticated user/device record;
- retries are idempotent where provider supports message IDs;
- payload must not expose sensitive secrets.


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
