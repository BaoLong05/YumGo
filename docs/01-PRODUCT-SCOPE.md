# 1. Product Scope and Functional Requirements

## 1.1 Actors

| Actor | Purpose |
|---|---|
| Guest | Browse public restaurants/menus and start registration/login |
| Customer | Manage profile/addresses/cart/orders/payment/reviews |
| Driver | Operate availability, accept delivery, update location and delivery state |
| RestaurantStaff | Operate branch menu and active orders |
| RestaurantManager | Staff administration and restaurant/branch operations |
| Operations | Cross-restaurant operational monitoring and intervention |
| Admin | Platform-wide configuration and access control |
| System | Executes notifications, reconciliation, expiry and cleanup jobs |
| PaymentProvider | External payment status source; communicated through signed webhooks |

## 1.2 Core use cases

### Customer

1. Register account.
2. Login and refresh session.
3. Manage profile and addresses.
4. Browse restaurants by location, status and search text.
5. View branch menu.
6. Add/update/remove cart items.
7. Apply/remove a promotion.
8. Place order from exactly one branch.
9. Choose COD or an online payment method supported by the deployment.
10. Cancel an eligible order.
11. Track order status.
12. Track assigned driver on a live map.
13. View order history and details.
14. Review completed orders.
15. Read and mark notifications.

### Restaurant staff

1. View assigned branch.
2. Update branch open/closed state.
3. Create/edit/archive menu categories and items.
4. Mark items sold out/available.
5. Receive incoming orders.
6. Confirm or reject eligible orders.
7. Move confirmed orders through preparation states.
8. Mark order ready for pickup.
9. View current orders by status.
10. View branch operational metrics.

### Driver

1. View verification/account status.
2. Go online/offline.
3. Receive only eligible delivery offers.
4. Accept/reject eligible delivery.
5. Navigate to restaurant.
6. Confirm pickup.
7. Stream current location while an active delivery exists.
8. Complete delivery with proof fields required by deployment.
9. Report delivery failure when permitted.
10. View delivery history and earnings summary.

### Operations/Admin

1. Search users/restaurants/drivers/orders.
2. Suspend/reactivate accounts.
3. Observe active deliveries and live locations.
4. Inspect failed payments and webhook events.
5. Replay safe outbox/message processing.
6. Manage roles and permissions.
7. Inspect audit/security events.
8. View dashboard metrics.

## 1.3 Business rules

### Restaurant

- A customer can only order from one restaurant branch per order.
- A branch must be open and accepting orders before a new order can be confirmed.
- A sold-out menu item cannot be added to a new cart item or accepted into a new order.
- Changing a current menu price does not change the price stored on an existing order.

### Cart

- One active cart exists per customer per branch.
- A cart cannot contain items from another branch.
- Quantity must be between 1 and the configured maximum; v1 maximum is 99 per line.
- Expired carts are read-only until recreated.

### Order

- The server calculates all monetary amounts.
- Client totals are treated as hints only.
- Order items store immutable snapshots of item name and unit price.
- Order status can only change through domain/application use cases.
- Customer cancellation is allowed only before the cancellation cutoff defined by the current state.
- A completed order cannot be cancelled.
- Delivery cannot be assigned to an order that is not in a delivery-eligible state.

### Payment

- No client-side payment-success flag is trusted.
- A payment webhook must be signature-verified and idempotent.
- Payment events are processed at most once by provider event ID.
- A successful online payment can move an order forward only after the payment transition is committed.

### Driver

- A driver may have at most one accepted active delivery in v1.
- A driver can publish location only while online and assigned/accepted for a delivery.
- A customer may subscribe only to tracking for their own order.

### Review

- Only the customer who owns a completed order can review it.
- One review exists per completed order.
- Reviews cannot be created before completion.

## 1.4 Functional acceptance rules

Every feature must define:
- authenticated actor
- authorization permission
- request shape
- validation rules
- business invariants
- persistence effect
- emitted events
- notifications if any
- error mapping
- idempotency behavior when relevant
- unit/integration tests
