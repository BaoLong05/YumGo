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

## API behavior
- Active, email-verified restaurant owners and active `manager`/`staff` assignments may manage the restaurant menu; every branch/category/item relationship is checked server-side.
- Categories are created active and may be updated to active/inactive. Category names are case-insensitively unique within a branch.
- Items are created active and available. `status` (`active`/`inactive`) is independent from `stock_status` (`available`/`unavailable`/`out_of_stock`).
- New items must reference an active category on the same branch. An inactive item cannot be published while its category is inactive. Existing active items may be edited while their category is inactive.
- Sold-out and available actions change only `stock_status`; repeating the current value is a no-op. Mutations and audit rows commit in one transaction; no notifications are sent.
- Current endpoints support category creation/update and item creation/update/availability; category deletion is not exposed. If deletion is added, block it while active items remain, as stated in the rule above.

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
