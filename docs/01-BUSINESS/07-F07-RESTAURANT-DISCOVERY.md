# F07 — Restaurant Discovery

**Actor:** Customer


### Inputs
search, district/city, openNow, category, cursor, limit.

### Validation
- limit bounded (e.g. 1..50);
- cursor treated as opaque token;
- only active restaurant/branches and publishable menu data returned;
- search does not reveal suspended/private records.


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
