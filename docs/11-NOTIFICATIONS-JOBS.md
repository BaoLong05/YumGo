# 11. Notifications and Background Jobs

## 11.1 Notification channels

The system supports three conceptual channels:
- in-app notification record
- push notification
- email/SMS when the deployment enables them

The business event is channel-independent.

## 11.2 Notification flow

```text
OrderConfirmed
   ↓
Outbox
   ↓
Message consumer
   ↓
Notification application service
   ├── create in-app notification
   └── enqueue push/email if enabled
```

## 11.3 Notification types

Customer:
- order received
- order confirmed
- order preparing
- order ready
- driver assigned
- order picked up
- driver near destination
- order delivered
- payment failed
- refund completed

Restaurant:
- new order
- customer cancellation
- payment issue

Driver:
- new delivery offer
- offer expiry
- reassignment/cancellation

Operations:
- payment mismatch
- queue failure
- excessive order failures
- driver allocation shortage

## 11.4 Durable processing

Critical asynchronous work must survive application restart.

Required pattern:

```text
Database transaction
   ├── business state
   └── outbox message
          ↓
   outbox publisher
          ↓
   durable broker
          ↓
   idempotent consumer
```

## 11.5 Required jobs

### Outbox publisher
Publishes unpublished outbox rows with retry and backoff.

### Payment reconciliation
Finds unresolved payments and compares provider state.

### Order timeout
Finds orders waiting beyond configured SLA and triggers the defined timeout policy.

### Cart cleanup
Marks expired carts read-only and removes associated ephemeral state.

### Driver-location cleanup
Expires Redis live tracking and prunes temporary location cache entries.

### Notification retry
Retries transient provider failures. Permanent failures are dead-lettered and visible to operations.

### Rating aggregation
Updates denormalized restaurant/driver rating summaries from completed reviews.

## 11.6 Retry policy

Retries are for transient failures only.

Default behavior:
- exponential backoff
- bounded attempts
- dead-letter after exhaustion
- preserve correlation ID
- record last error

## 11.7 Idempotency requirement

Every consumer must tolerate duplicate message delivery.

Use a unique business key, event ID, or processed-message table/state to ensure effects are applied once.
