# 6. HTTP API Specification

Base URL:

`/api/v1`

All JSON timestamps are ISO-8601 UTC.

## 6.1 Standard success envelope

Single resource:

```json
{
  "data": { }
}
```

Collection:

```json
{
  "data": [],
  "meta": {
    "nextCursor": null,
    "hasMore": false
  }
}
```

## 6.2 Authentication

| Method | Route | Auth |
|---|---|---|
| POST | `/auth/register` | Public |
| POST | `/auth/login` | Public |
| POST | `/auth/refresh` | Public + refresh token |
| POST | `/auth/logout` | Authenticated |
| GET | `/auth/me` | Authenticated |

### Register request

```json
{
  "email": "long@example.com",
  "phone": "+84900000000",
  "password": "strong-password",
  "fullName": "Long Tran"
}
```

Validation:
- valid normalized email
- password meets configured password policy
- phone valid if supplied
- email/phone unique

Returns `201 Created` with public user profile and access/refresh token strategy defined by deployment.

## 6.3 Customer profile/addresses

| Method | Route |
|---|---|
| GET | `/me/profile` |
| PATCH | `/me/profile` |
| GET | `/me/addresses` |
| POST | `/me/addresses` |
| PATCH | `/me/addresses/{id}` |
| DELETE | `/me/addresses/{id}` |
| POST | `/me/addresses/{id}/default` |

Resource ownership is mandatory.

## 6.4 Restaurant discovery

| Method | Route |
|---|---|
| GET | `/restaurants` |
| GET | `/restaurants/{restaurantId}` |
| GET | `/restaurants/{restaurantId}/branches` |
| GET | `/branches/{branchId}` |
| GET | `/branches/{branchId}/menu` |

Query parameters for `/restaurants`:
- `cursor`
- `limit` (1–50, default 20)
- `q`
- `latitude`
- `longitude`
- `radiusKm`
- `status=open`
- `sort=distance|rating|popular`

The server must clamp limits and reject invalid cursor values.

## 6.5 Cart

| Method | Route |
|---|---|
| GET | `/cart?branchId={id}` |
| POST | `/cart/items` |
| PATCH | `/cart/items/{id}` |
| DELETE | `/cart/items/{id}` |
| DELETE | `/cart/items` |
| POST | `/cart/promotion` |
| DELETE | `/cart/promotion` |

`POST /cart/items` request:

```json
{
  "branchId": "uuid",
  "menuItemId": "uuid",
  "quantity": 2,
  "note": "Less spicy"
}
```

The server verifies that the item belongs to the selected branch menu and is currently orderable.

## 6.6 Orders

| Method | Route | Actor |
|---|---|---|
| POST | `/orders` | Customer |
| GET | `/orders` | Customer |
| GET | `/orders/{id}` | Owner/restaurant/ops as authorized |
| POST | `/orders/{id}/cancel` | Customer/authorized operator |
| GET | `/orders/{id}/tracking` | Owner/driver/ops as authorized |

### Place order request

```json
{
  "cartId": "uuid",
  "addressId": "uuid",
  "paymentMethod": "cod",
  "customerNote": "Ring the bell"
}
```

Required server-side sequence:

1. Load active cart.
2. Re-check branch/menu/item availability.
3. Recalculate prices.
4. Validate promotion again.
5. Calculate delivery/service fees.
6. Create order + immutable item snapshots.
7. Create payment record if required.
8. Write outbox events in the same transaction.
9. Return order representation.

## 6.7 Restaurant operations

| Method | Route |
|---|---|
| GET | `/restaurant/orders` |
| GET | `/restaurant/orders/{id}` |
| POST | `/restaurant/orders/{id}/accept` |
| POST | `/restaurant/orders/{id}/reject` |
| POST | `/restaurant/orders/{id}/start-preparing` |
| POST | `/restaurant/orders/{id}/ready` |
| GET | `/restaurant/branches/{branchId}/menu` |
| POST | `/restaurant/menu/categories` |
| PATCH | `/restaurant/menu/categories/{id}` |
| POST | `/restaurant/menu/items` |
| PATCH | `/restaurant/menu/items/{id}` |
| POST | `/restaurant/menu/items/{id}/sold-out` |
| POST | `/restaurant/menu/items/{id}/available` |

Every route applies restaurant/branch resource scope.

## 6.8 Driver operations

| Method | Route |
|---|---|
| POST | `/driver/availability/online` |
| POST | `/driver/availability/offline` |
| GET | `/driver/deliveries/offers` |
| POST | `/driver/deliveries/{id}/accept` |
| POST | `/driver/deliveries/{id}/reject` |
| POST | `/driver/deliveries/{id}/pickup` |
| POST | `/driver/deliveries/{id}/complete` |
| POST | `/driver/deliveries/{id}/fail` |
| POST | `/driver/deliveries/{id}/location` |
| GET | `/driver/deliveries` |

Location request:

```json
{
  "latitude": 10.123456,
  "longitude": 106.123456,
  "accuracyM": 8.5,
  "speedMps": 6.2,
  "headingDeg": 180
}
```

The backend rejects location updates that fail driver/delivery scope checks or contain invalid coordinates.

## 6.9 Payments

| Method | Route |
|---|---|
| POST | `/orders/{id}/payment-intent` |
| GET | `/orders/{id}/payment` |
| POST | `/payments/webhooks/{provider}` |
| POST | `/payments/{id}/reconcile` |

Webhook endpoints are unauthenticated at the application user level and instead use provider signature verification + event ID idempotency.

## 6.10 Notifications

| Method | Route |
|---|---|
| GET | `/notifications` |
| POST | `/notifications/{id}/read` |
| POST | `/notifications/read-all` |

## 6.11 Reviews

| Method | Route |
|---|---|
| POST | `/orders/{id}/review` |
| GET | `/restaurants/{id}/reviews` |

## 6.12 Operations/admin

| Method | Route |
|---|---|
| GET | `/ops/dashboard/overview` |
| GET | `/ops/orders` |
| GET | `/ops/deliveries` |
| GET | `/ops/drivers` |
| GET | `/ops/payments/failures` |
| GET | `/ops/audit-events` |
| POST | `/admin/users/{id}/suspend` |
| POST | `/admin/users/{id}/restore` |
| POST | `/admin/roles/{id}/permissions` |

## 6.13 HTTP status policy

- `200` successful read/update with body
- `201` resource created
- `204` successful action with no body
- `400` malformed request or invalid state for transport-level input
- `401` missing/invalid authentication
- `403` authenticated but unauthorized
- `404` resource not visible/not found
- `409` state conflict/idempotency/business concurrency conflict
- `422` semantically invalid request shape when the API chooses to distinguish it from 400
- `429` rate limit
- `500` unexpected server failure
- `503` dependency/service unavailable

## 6.14 Pagination

Cursor pagination is the default for high-volume collections.

The API must return a stable cursor generated from a deterministic sort tuple, for example `(created_at, id)`.
