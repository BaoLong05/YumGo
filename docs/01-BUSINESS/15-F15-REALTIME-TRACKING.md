# F15 — Real-time Tracking

**Actor:** Driver / Customer / Operations


### Driver update
Payload contains deliveryId, latitude, longitude, recordedAt.

### Validation
- authenticated driver must own current assignment;
- coordinate range valid;
- timestamp cannot be arbitrarily far in the future;
- update frequency throttled;
- no location update after delivery completion.

### Customer visibility
Customer may subscribe only to tracking channel for an order they own.
Current location may be cached in Redis; durable history is sampled into PostgreSQL.


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
