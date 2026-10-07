# 8. Payment Specification

## 8.1 Payment architecture

Payment is an external boundary.

```text
Application
   ↓
IPaymentProvider
   ↓
Concrete provider adapter
   ↓
Payment gateway/bank service
   ↓
Signed webhook
   ↓
YumGo webhook endpoint
   ↓
Verify → dedupe → reconcile
```

## 8.2 Supported v1 methods

- `cod`
- `online` via one configured provider adapter

The API contract does not expose provider-specific fields to the domain.

## 8.3 Payment intent

For online payment:

1. Order must exist.
2. Amount must equal the server-calculated order total.
3. Existing active payment intents for the same order are reused when safe.
4. An idempotency key is generated/stored.
5. Provider response is stored only as non-authoritative metadata.
6. Client receives the provider checkout instruction/token required by that provider.

## 8.4 Webhook verification

The webhook handler must:

1. Read the raw request body.
2. Validate required provider headers.
3. Verify HMAC/signature using a server-side secret.
4. Reject invalid signatures.
5. Check provider event ID uniqueness.
6. Persist the event.
7. Reconcile the referenced payment/order.
8. Commit the state change and event-processing marker atomically.
9. Return the provider-required acknowledgement.

## 8.5 Idempotent webhook behavior

Given the same `provider + provider_event_id`, processing the event more than once must produce the same final database state and must not duplicate refunds, notifications or order transitions.

## 8.6 Payment/order consistency

Examples:

- `Payment = Paid` cannot be moved back to `Pending`.
- A failed payment cannot silently mark the order as confirmed.
- A refunded payment must be linked to an eligible order/payment state.
- A duplicated success webhook must be a no-op after the first successful processing.

## 8.7 Reconciliation

A scheduled reconciliation job queries provider state for payments that remain unresolved beyond the configured time window.

It compares:
- provider payment state
- YumGo payment state
- YumGo order state

Any mismatch becomes an operations alert and a controlled repair workflow, not an automatic destructive update.

## 8.8 Security

Never store:
- card PAN
- CVV
- raw authentication secrets from the provider
- access tokens in application logs

Store only provider references, signed event payloads where permitted, and the minimum metadata needed for reconciliation/audit.
