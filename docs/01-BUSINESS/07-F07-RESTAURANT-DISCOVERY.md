# F07 — Restaurant Discovery

**Actor:** Customer

## Implemented API slice
- Public restaurant search supports text, city, district, `openNow`, active menu category, and cursor/limit filters.
- Restaurant/branch detail and branch-list routes expose only active restaurants and open, non-deleted branches.
- Public branch menu returns active categories and only active, available, non-deleted items.
- Results use a deterministic cursor; limit defaults to 20 and is bounded to 1..50.
- The current schema has no timezone field, so `openNow` compares same-day opening hours with UTC. Branches without configured hours are treated as open when branch status is open.
- Geospatial radius and distance/rating/popularity sorting and menu cache are explicitly pending; the routes do not interpret those API parameters yet.


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
