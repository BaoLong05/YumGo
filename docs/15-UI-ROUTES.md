# 15. Web and Mobile Route/Screen Specification

## 15.1 Web route groups

The single Next.js web app uses role-aware route groups.

### Public/customer

- `/`
- `/restaurants`
- `/restaurants/[restaurantId]`
- `/branches/[branchId]/menu`
- `/cart`
- `/checkout`
- `/orders`
- `/orders/[orderId]`
- `/orders/[orderId]/tracking`
- `/profile`
- `/profile/addresses`
- `/notifications`

### Restaurant

- `/restaurant`
- `/restaurant/orders`
- `/restaurant/orders/[orderId]`
- `/restaurant/menu`
- `/restaurant/menu/categories`
- `/restaurant/branches`

### Operations

- `/ops`
- `/ops/orders`
- `/ops/deliveries`
- `/ops/drivers`
- `/ops/payments`
- `/ops/audit`

### Admin

- `/admin/users`
- `/admin/roles`
- `/admin/permissions`
- `/admin/restaurants`
- `/admin/settings`

## 15.2 Mobile route groups

Exactly one mobile application.

### Unauthenticated

- `/(auth)/login`
- `/(auth)/register`

### Customer role

- `/(customer)/home`
- `/(customer)/restaurants`
- `/(customer)/restaurant/[id]`
- `/(customer)/cart`
- `/(customer)/checkout`
- `/(customer)/orders`
- `/(customer)/orders/[id]`
- `/(customer)/orders/[id]/tracking`
- `/(customer)/profile`

### Driver role

- `/(driver)/home`
- `/(driver)/offers`
- `/(driver)/delivery/[id]`
- `/(driver)/history`
- `/(driver)/profile`

Navigation is not used as the authorization boundary; the backend remains authoritative.

## 15.3 Loading/empty/error states

Every network-backed screen must define:
- loading state
- empty state
- error state
- retry behavior
- unauthorized state
- offline behavior where applicable

## 15.4 Tracking screen

Customer tracking screen must display:
- order status timeline
- restaurant/branch
- assigned driver when available
- current driver position when available
- ETA when available
- delivery address summary

The UI must tolerate missing/stale location without treating it as order failure.
