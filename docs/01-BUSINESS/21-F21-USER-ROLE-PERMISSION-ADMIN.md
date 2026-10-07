# F21 — User / Role / Permission Admin

**Actor:** Admin


### Rules
- admin-only by policy;
- role code must exist;
- permission code must exist;
- user-role assignment unique;
- administrator cannot accidentally remove the last usable admin without explicit safeguard;
- every change audited.

IDs are NanoID; role/permission names are not resource identities in URLs where a NanoID route can be used.


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
