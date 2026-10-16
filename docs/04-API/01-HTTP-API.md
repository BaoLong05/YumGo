> CANONICAL RULES: PostgreSQL is the persistence database. All business/resource IDs are NanoID(21). Hidden form IDs and URL IDs are untrusted input and require ownership/authorization validation.

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
  "phone": "+84912340000",
  "password": "strong-password",
  "fullName": "Long Tran"
}
```

Validation:
- valid normalized email
- password is at least 6 characters and has at least 3 of 4 character groups (uppercase, lowercase, digit, symbol)
- phone is valid and normalized to E.164 form if supplied
- email/phone unique under current database constraints

Returns `201 Created` with `{ "data": { "id", "email", "fullName" } }`. Registration does not issue access or refresh tokens; those belong to the separate login/refresh flows. Duplicate email and phone both return the same generic `409 Conflict` response to avoid account enumeration. Invalid fields return `422 Unprocessable Content`; the in-process limiter allows at most 5 requests per minute per observed client IP on each API instance and returns `429 Too Many Requests` after that limit. Multi-instance deployments need a shared limiter or gateway policy for a global quota.

### Login request and response

`POST /api/v1/auth/login` accepts `{ "email": "long@example.com", "password": "..." }`. Success returns `200 OK` in the standard envelope:

```json
{
  "data": {
    "accessToken": "<signed-jwt>",
    "accessTokenExpiresAt": "<UTC timestamp>",
    "refreshToken": "<opaque-token>",
    "refreshTokenExpiresAt": "<UTC timestamp>",
    "tokenType": "Bearer"
  }
}
```

Invalid, missing, disabled, or suspended accounts receive the same generic `401 Unauthorized` shape. Login is limited to 5 requests per minute per observed client IP per API instance.

### Refresh and logout

`POST /api/v1/auth/refresh` accepts `{ "refreshToken": "<current-opaque-token>" }` and returns the same token envelope. A successful refresh rotates the token, invalidates its predecessor, and preserves the original `refreshTokenExpiresAt` (48 hours after login); it does not extend the session. Invalid, expired, or replayed tokens return generic `401 Unauthorized`. Replaying a rotated token revokes its descendant chain. Refresh is limited to 10 requests per minute per observed client IP per API instance.

`POST /api/v1/auth/logout` requires both `Authorization: Bearer <accessToken>` and `{ "refreshToken": "<current-opaque-token>" }`; success returns `204 No Content`. The refresh token must match the JWT's current session. Logout revokes that session, and the API rejects its access JWT immediately on subsequent authenticated requests. Missing refresh token returns `400 Bad Request`.

`GET /api/v1/auth/me` requires a valid bearer token and returns `{ "data": { "id": "<user-id>", "roles": ["<database-role>"] } }`; it does not expose profile or address data.

Access JWT lifetime defaults to 15 minutes (configurable from 1 to 60 minutes) and is capped by the absolute 48-hour session deadline. The API requires `Authentication__Jwt__Issuer`, `Authentication__Jwt__Audience`, and `Authentication__Jwt__SigningKey` at runtime; the signing key is base64-encoded and at least 32 bytes. See the authentication security specification for deployment and proxy/rate-limit considerations.

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

The address endpoints are authenticated and scoped to the user in the access token. Create requires `label`, `recipientName`, `recipientPhone`, `latitude`, and `longitude`; `isDefault` is optional (defaults to false). The API intentionally has no client-supplied street/city fields. The backend reverse-geocodes coordinates using Google Geocoding and returns the provider-derived address parts. `PATCH` may update label/recipient details without coordinates; changing location requires both coordinates and triggers a new reverse-geocode request.

Create returns `201`, update/list return `200`, and delete/set-default return `204`. Invalid coordinates or an unresolved delivery address return `422`; an address not owned by the caller returns `404`; Google lookup failures return `503`; the protected monthly lookup ceiling returns `429`.

Set `GoogleMaps__GeocodingApiKey` as a backend secret. `GoogleMaps__MonthlyRequestLimit` defaults to `9000` and accepts only `1..9000`. A PostgreSQL counter enforces this limit per UTC calendar month across app instances. Google currently lists a 10,000-request monthly free usage cap for the Geocoding SKU, but billing must still be enabled; the app's cap does not include usage from other projects on the same billing account. Configure Google Cloud quota and billing alerts separately before enabling the key.

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
  "branchId": "NanoID(21)",
  "menuItemId": "NanoID(21)",
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
  "cartId": "NanoID(21)",
  "addressId": "NanoID(21)",
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
