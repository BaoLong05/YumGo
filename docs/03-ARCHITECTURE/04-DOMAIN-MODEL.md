# 4. Domain Model and Aggregates

## 4.1 Value objects

### `Money`

Fields:
- `long AmountMinor`
- `string Currency`

Rules:
- amount must be non-negative where used for prices
- currency must be ISO-style uppercase code in v1
- arithmetic must preserve currency compatibility

### `Address`

Fields:
- recipient name
- recipient phone
- line1
- ward
- district
- province
- latitude
- longitude
- delivery note

A normalized address may belong to a customer. Orders store an immutable address snapshot.

### `GeoCoordinate`

Fields:
- latitude
- longitude

Rules:
- latitude ∈ [-90, 90]
- longitude ∈ [-180, 180]

### `OrderNumber`

Human-readable identifier. Format:

`YG-YYYYMMDD-XXXXXX`

The numeric suffix is generated server-side and uniqueness is enforced by the database.

### `Email` / `PhoneNumber`

Normalize before lookup. Preserve the original presentation only where required for display.

## 4.2 Aggregates

### Order aggregate

Root: `Order`

Children:
- OrderItem

Responsible for:
- status transition
- adding/removing items before confirmation
- cancellation policy
- totals consistency
- emitting order lifecycle events

External code cannot mutate `OrderItem` directly.

### Cart aggregate

Root: `Cart`

Children:
- CartItem

Responsible for:
- item quantity changes
- branch consistency
- cart expiry

### Menu aggregate

Root: `Menu`

Children:
- MenuCategory
- MenuItem

Responsible for:
- category/item ordering
- active/sold-out state
- publication state

### Delivery aggregate

Root: `Delivery`

Children:
- DeliveryAssignment

Responsible for:
- assignment state
- pickup state
- delivery completion
- failure/cancellation rules

### Promotion aggregate

Root: `Promotion`

Responsible for:
- validity window
- eligibility constraints
- usage rules

### Restaurant aggregate

Root: `Restaurant`

Children/entities:
- RestaurantBranch
- RestaurantStaff assignment references

Responsible for:
- restaurant lifecycle
- branch lifecycle
- restaurant-level policy

## 4.3 Entities outside major aggregates

- User
- CustomerProfile
- DriverProfile
- Payment
- Review
- Notification
- AuditEvent
- OutboxMessage

These may be persisted independently because they do not need to participate in every transaction that touches their related domain objects.

## 4.4 Order state machine

```text
PendingPayment
     │
     ├──> Cancelled
     │
     └──> PendingConfirmation
               │
               ├──> Rejected
               ├──> Cancelled
               └──> Confirmed
                     │
                     ├──> Cancelled
                     └──> Preparing
                           │
                           ├──> Cancelled (operator exception + compensation flow)
                           └──> ReadyForPickup
                                  │
                                  └──> Assigned
                                         │
                                         └──> PickedUp
                                                │
                                                └──> Delivering
                                                       │
                                                       ├──> Failed
                                                       └──> Delivered
```

COD orders may skip `PendingPayment` and start at `PendingConfirmation`.

## 4.5 Delivery state machine

```text
WaitingForAssignment
        ↓
     Assigned
        ├──> Rejected → WaitingForAssignment
        └──> Accepted
               ↓
       ArrivingAtRestaurant
               ↓
            PickedUp
               ↓
           Delivering
             ├──> Delivered
             └──> Failed
```

## 4.6 Payment state machine

```text
Pending
  ├──> Paid
  ├──> Failed
  └──> Cancelled

Paid
  ├──> Refunded
  └──> PartiallyRefunded
```

## 4.7 Domain events

Canonical events:
- `UserRegistered`
- `OrderPlaced`
- `OrderConfirmed`
- `OrderRejected`
- `OrderPreparing`
- `OrderReadyForPickup`
- `DeliveryAssigned`
- `DeliveryAccepted`
- `DeliveryPickedUp`
- `DeliveryCompleted`
- `OrderDelivered`
- `OrderCancelled`
- `PaymentSucceeded`
- `PaymentFailed`
- `PaymentRefunded`
- `PromotionApplied`
- `ReviewCreated`

Events are facts, not commands.
