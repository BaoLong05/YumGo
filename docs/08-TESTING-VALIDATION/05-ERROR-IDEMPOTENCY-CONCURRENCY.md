# 16. Errors, Idempotency and Concurrency

## 16.1 ProblemDetails

Errors use a consistent ProblemDetails-style response.

Example:

```json
{
  "type": "https://api.yumgo.local/problems/order-state-conflict",
  "title": "Order state conflict",
  "status": 409,
  "code": "ORDER_STATE_CONFLICT",
  "detail": "The order cannot be moved from its current state.",
  "traceId": "00-..."
}
```

The client uses `code` for stable behavior. `detail` is human-readable and may change.

## 16.2 Error codes

Canonical examples:
- `AUTH_INVALID_CREDENTIALS`
- `AUTH_ACCOUNT_SUSPENDED`
- `RESOURCE_NOT_FOUND`
- `RESOURCE_FORBIDDEN`
- `VALIDATION_ERROR`
- `CART_BRANCH_MISMATCH`
- `MENU_ITEM_UNAVAILABLE`
- `ORDER_STATE_CONFLICT`
- `ORDER_CANCELLATION_NOT_ALLOWED`
- `PAYMENT_FAILED`
- `PAYMENT_EVENT_DUPLICATE`
- `DELIVERY_ALREADY_ASSIGNED`
- `DRIVER_NOT_ELIGIBLE`
- `CONCURRENT_MODIFICATION`
- `IDEMPOTENCY_CONFLICT`
- `RATE_LIMITED`

## 16.3 Idempotency keys

Required for:
- order creation
- payment intent creation
- payment webhook processing by provider event ID
- delivery acceptance
- delivery completion when client retries can occur

Client-originated idempotency key:

`Idempotency-Key: <opaque random value>`

The server stores the key with the operation's stable result or enough metadata to detect a conflicting request.

## 16.4 Idempotency conflict

Same key + same canonical request → return the original result.

Same key + different request fingerprint → return `409 IDEMPOTENCY_CONFLICT`.

## 16.5 Optimistic concurrency

Orders, deliveries and other stateful aggregates contain a `version` field.

Update statements use the expected version:

```text
UPDATE ...
SET version = version + 1
WHERE id = @id AND version = @expectedVersion;
```

Zero affected rows means a concurrent modification and maps to `409 CONCURRENT_MODIFICATION`.

## 16.6 Double-accept prevention

When two drivers accept the same delivery offer concurrently, only one transaction may win.

The application uses a database constraint/conditional update to establish the winner. The losing transaction returns a conflict response.

Do not rely on a C# process-local lock because multiple application instances may run.

## 16.7 Distributed locking

A distributed lock is not the default solution. Use database uniqueness/conditional updates first. Redis distributed locking is allowed only for operations where a database invariant cannot express the required coordination.
