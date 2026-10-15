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
