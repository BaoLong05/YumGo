# F23 — Edge Cases

**Actor:** All


Minimum scenarios:
- duplicate registration;
- two simultaneous checkout requests;
- item becomes unavailable between checkout and placement;
- restaurant closes while cart exists;
- promotion expires between preview and order;
- payment webhook delivered twice;
- refund requested twice;
- driver accepts after another driver already accepted;
- customer requests another customer's order by NanoID;
- hidden form field is tampered;
- role claim in client payload is forged;
- stale update on order state;
- address deleted after being selected for checkout.

Every scenario must have an expected status/result and must not corrupt relational integrity.


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
