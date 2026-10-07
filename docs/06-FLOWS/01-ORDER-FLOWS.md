# 7. End-to-End Order and Delivery Flows

## 7.1 Customer checkout

```text
Customer
  ↓
GET menu
  ↓
Cart
  ↓
Apply promotion
  ↓
POST /orders
  ↓
Revalidate cart + price + availability
  ↓
Transaction
  ├── create order
  ├── create order items
  ├── create payment (if needed)
  └── create outbox event
  ↓
Commit
  ↓
Response
```

## 7.2 COD order

```text
POST /orders
   ↓
PendingConfirmation
   ↓
Restaurant accepts
   ↓
Confirmed
   ↓
Preparing
   ↓
ReadyForPickup
   ↓
Delivery assigned
   ↓
PickedUp
   ↓
Delivering
   ↓
Delivered
```

COD payment becomes `Paid` at the business point defined by the deployment, normally when delivery is completed.

## 7.3 Online payment order

```text
Create order
   ↓
PendingPayment
   ↓
Create payment intent
   ↓
External provider
   ↓
Customer completes payment
   ↓
Signed webhook
   ↓
Verify + deduplicate
   ↓
Payment = Paid
   ↓
Order = PendingConfirmation
   ↓
Restaurant workflow continues
```

A callback redirect from the browser/mobile app is informative only. The signed server-to-server event is authoritative.

## 7.4 Restaurant acceptance

Preconditions:
- order is `PendingConfirmation`
- branch is open and accepting orders
- payment state is compatible with the order's payment method

Effects:
- order → `Confirmed`
- `confirmed_at` set
- outbox `OrderConfirmed`
- notify customer

## 7.5 Preparation

`Confirmed → Preparing → ReadyForPickup`.

Only authorized restaurant staff can perform these transitions.

## 7.6 Delivery assignment

Candidate selection must satisfy:
- driver is online
- driver verified and not suspended
- no active delivery in v1
- location is sufficiently recent
- driver is inside configured service region

Selection strategy:
1. filter eligible drivers
2. rank by route distance/time or geo distance
3. create an assignment offer
4. notify the selected driver
5. expire the offer after the configured window
6. retry next candidate on reject/expiry

The selection algorithm remains in Application/Domain policy, while map-distance calculation is an infrastructure service.

## 7.7 Pickup

Driver must have an accepted assignment and order must be `ReadyForPickup`.

On success:
- delivery → `PickedUp`
- order → `PickedUp`
- pickup timestamp set
- customer notified

## 7.8 Live tracking

```text
Driver app
  ↓ location update
API validates driver + delivery ownership
  ↓
Redis current location
  ↓
SignalR group `order:{orderId}`
  ↓
Customer app/web receives location
```

Recommended v1 update behavior:
- send only while active delivery exists
- ignore obvious invalid jumps
- coalesce updates when faster than the server's broadcast interval
- persist sampled history, not every GPS event

## 7.9 Delivery completion

Preconditions:
- delivery is `Delivering`
- assigned driver matches actor
- order is not already delivered/cancelled

Effects:
- delivery → `Delivered`
- order → `Delivered`
- payment may be marked paid for COD
- customer receives completion notification
- review becomes eligible
- driver becomes available after transaction completion

## 7.10 Cancellation

Customer cancellation endpoint checks the current state and cancellation policy.

Restaurant/operations cancellation after preparation begins requires an explicit reason and triggers a compensation/refund decision when money has already been captured.

Cancellation is never implemented as a direct SQL status update from a controller.
