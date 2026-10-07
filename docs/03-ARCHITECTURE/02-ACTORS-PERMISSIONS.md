# 2. Actors, Roles and Permissions

## 2.1 Roles

| Role | Scope |
|---|---|
| Customer | Self-owned customer resources |
| Driver | Self + currently assigned delivery |
| RestaurantStaff | One restaurant/branch assigned to the staff member |
| RestaurantManager | One restaurant and its branches |
| Operations | Platform operational resources without security-administration rights |
| Admin | Platform-wide |

A user may hold multiple roles. Authorization is evaluated as: authenticated user → role → permission → resource scope.

## 2.2 Permission catalogue

### Identity

- `identity.profile.read`
- `identity.profile.update`
- `identity.user.suspend`
- `identity.user.restore`
- `identity.role.assign`

### Restaurant/menu

- `restaurant.read`
- `restaurant.manage`
- `restaurant.branch.read`
- `restaurant.branch.manage`
- `restaurant.menu.read`
- `restaurant.menu.manage`
- `restaurant.order.read`
- `restaurant.order.accept`
- `restaurant.order.reject`
- `restaurant.order.prepare`
- `restaurant.order.ready`

### Customer

- `customer.address.read`
- `customer.address.manage`
- `customer.cart.read`
- `customer.cart.manage`
- `customer.order.create`
- `customer.order.read`
- `customer.order.cancel`
- `customer.review.create`

### Driver

- `driver.profile.read`
- `driver.availability.manage`
- `driver.delivery.read`
- `driver.delivery.accept`
- `driver.delivery.reject`
- `driver.delivery.pickup`
- `driver.delivery.complete`
- `driver.location.publish`

### Payment/operations

- `payment.read.self`
- `payment.webhook.process`
- `payment.reconcile`
- `ops.dashboard.read`
- `ops.order.read`
- `ops.delivery.read`
- `ops.delivery.intervene`
- `ops.audit.read`

## 2.3 Scope rules

### Customer

Can access only resources where `resource.CustomerId == User.Id`.

### Driver

Can access a delivery only when `delivery.AssignedDriverId == User.Id`, except operations roles.

### RestaurantStaff

Can access only branches belonging to their assigned restaurant. A branch-level staff assignment narrows access further.

### RestaurantManager

Can access all branches of their restaurant.

### Operations

Can inspect platform operational data. Operations cannot modify role definitions or administrator accounts.

### Admin

Platform-wide access subject to explicit permission checks.

## 2.4 Authorization implementation rule

Do not rely solely on route naming or frontend visibility. Backend policies must check authenticated identity, permission and resource scope.
