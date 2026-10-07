# F10 — Checkout

**Actor:** Customer


### Steps
1. Load authenticated user's active cart.
2. Validate selected address ownership.
3. Reload menu prices/statuses.
4. Recalculate subtotal.
5. Validate promotion.
6. Calculate delivery fee.
7. Calculate final total.
8. Present server-calculated summary.

Client may send `addressId`, `promotionCode`, payment method; it may not send final total as authoritative.


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
