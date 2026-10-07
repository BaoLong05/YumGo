# F13 — Driver Availability / Assignment

**Actor:** Driver / Operations


### Driver online
Driver must have eligible account, valid driver profile and required availability state.

### Assignment
- delivery must be pending/assignable;
- driver must be online and not busy;
- system must ensure only one accepted active assignment;
- driver can accept only an offer addressed to that driver;
- duplicate accept is idempotent;
- rejected/expired offers cannot later be accepted.


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
