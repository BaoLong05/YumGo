# F06 — Menu

**Actor:** Restaurant Owner / Staff


### Rules
- category must belong to same branch as item;
- item price >= 0;
- inactive category cannot have a newly published active item;
- deleting category with active menu items is blocked until items are reassigned/deactivated;
- item status and stock_status are separate concepts.

### Hidden IDs
`branchId`, `categoryId`, `menuItemId` may be hidden transport state but all are revalidated server-side.


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
