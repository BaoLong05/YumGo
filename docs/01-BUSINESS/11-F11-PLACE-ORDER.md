# F11 — Place Order

**Actor:** Customer


### Preconditions
- active customer;
- active cart;
- cart non-empty;
- branch open and accepting orders;
- each item still purchasable;
- selected address belongs to customer;
- promotion still valid;
- payment method allowed.

### Transaction
In one transaction:
1. create order;
2. create order_items with price/name snapshots;
3. create payment pending/processing record;
4. copy address snapshot;
5. mark cart checked_out.

### Consistency
`total_amount = subtotal - discount + delivery_fee + tax`.
At least one order item required. No client-supplied price or total is trusted.

### Idempotency
Required using `Idempotency-Key`. Same key + same request hash returns same result. Same key + different request hash returns conflict.

### Acceptance
Exactly one business order for one idempotency key.


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
