# F16 — Payment

**Actor:** Customer / Payment Provider / Operations


### Initiation
Server creates payment intent from server-calculated order total.

### Webhook
- verify provider signature;
- verify provider event id uniqueness;
- compare order/payment identity;
- compare amount/currency;
- only allow legal payment state transition;
- process duplicate webhook idempotently.

### Never trust
A client field like `paymentStatus=paid` is never authoritative.


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
