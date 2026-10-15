# F17 — Cancel / Refund

**Actor:** Customer / Restaurant / Operations


### Cancellation
Allowed states depend on order stage and policy.

Examples:
- pending: customer can cancel;
- confirmed: may cancel with policy constraints;
- preparing: cancellation may require restaurant/operations;
- picked_up/delivering: customer self-cancel is not allowed.

### Refund validation
- order/payment must be eligible;
- amount > 0;
- cumulative successful refunds <= successful payment amount;
- provider refund ID unique;
- refund transition audited.


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
