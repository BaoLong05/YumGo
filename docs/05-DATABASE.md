# 5. PostgreSQL Database Specification

## 5.1 Database standards

- PostgreSQL is the system of record.
- All timestamps are UTC `timestamptz`.
- Application IDs use UUID.
- Money is stored as integer minor units (`bigint`) plus a currency code.
- Soft deletion is used only where history or audit requires preservation.
- Foreign keys are explicit.
- Unique business identifiers are database-enforced.
- JSONB is used only for snapshots/provider payloads/event data, not as a substitute for relational fields that are queried routinely.

## 5.2 Identity tables

### `users`

| Column | Type | Constraint |
|---|---|---|
| id | uuid | PK |
| email | citext | UNIQUE |
| phone | varchar(32) | UNIQUE, nullable |
| password_hash | text | NOT NULL |
| status | varchar(32) | NOT NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |
| last_login_at | timestamptz | nullable |

### `roles`

`id uuid PK`, `code varchar UNIQUE`, `name varchar`, `created_at timestamptz`.

### `permissions`

`id uuid PK`, `code varchar UNIQUE`, `name varchar`, `created_at timestamptz`.

### `user_roles`

Composite PK `(user_id, role_id)` with FKs to users and roles.

### `role_permissions`

Composite PK `(role_id, permission_id)` with FKs to roles and permissions.

### `refresh_tokens`

| Column | Type | Constraint |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK users |
| token_hash | text | UNIQUE, NOT NULL |
| expires_at | timestamptz | NOT NULL |
| revoked_at | timestamptz | nullable |
| created_at | timestamptz | NOT NULL |
| created_by_ip | inet | nullable |
| user_agent | text | nullable |

Never store a raw refresh token.

## 5.3 Customer tables

### `customer_profiles`

`user_id uuid PK/FK users`, `full_name varchar(160)`, `avatar_url text nullable`, `created_at`, `updated_at`.

### `addresses`

Fields:
- `id uuid PK`
- `user_id uuid FK`
- `label varchar(80)`
- `recipient_name varchar(160)`
- `recipient_phone varchar(32)`
- `line1 varchar(255)`
- `ward varchar(120)`
- `district varchar(120)`
- `province varchar(120)`
- `latitude numeric(9,6)`
- `longitude numeric(9,6)`
- `delivery_note varchar(500) nullable`
- `is_default boolean NOT NULL DEFAULT false`
- `created_at`, `updated_at`

Index: `(user_id, is_default DESC)`.

## 5.4 Restaurant/menu tables

### `restaurants`

`id`, `name`, `slug UNIQUE`, `status`, `phone`, `description`, `created_at`, `updated_at`.

### `restaurant_branches`

`id`, `restaurant_id FK`, `name`, `phone`, address fields, `latitude`, `longitude`, `opening_hours jsonb`, `status`, `accepting_orders boolean`, `created_at`, `updated_at`.

Index `(restaurant_id, status)`.

### `restaurant_staff`

`user_id`, `restaurant_id`, `branch_id nullable`, `role_code`, `created_at`.

Primary key `(user_id, restaurant_id, branch_id)` after normalizing the nullable branch identity as needed by the migration; a partial unique index is used for branch-specific assignment rules.

### `menus`

`id`, `branch_id`, `name`, `status`, `published_at nullable`, `created_at`, `updated_at`.

### `menu_categories`

`id`, `menu_id`, `name`, `sort_order int`, `status`, timestamps.

### `menu_items`

`id`, `category_id`, `name`, `description`, `price_minor bigint`, `currency char(3)`, `prep_minutes smallint`, `status`, `image_url`, timestamps.

Indexes: `(category_id, status, sort_order)` and searchable name index.

## 5.5 Cart/order tables

### `carts`

`id`, `user_id`, `branch_id`, `status`, `expires_at`, timestamps.

Unique partial index: one active cart per `(user_id, branch_id)`.

### `cart_items`

`id`, `cart_id`, `menu_item_id`, `quantity`, `unit_price_minor`, `currency`, `note`, timestamps.

Unique `(cart_id, menu_item_id)` for v1; customization data is stored in `selected_options jsonb` when enabled.

### `orders`

| Column | Type |
|---|---|
| id | uuid PK |
| order_number | varchar(32) UNIQUE |
| customer_id | uuid FK users |
| branch_id | uuid FK restaurant_branches |
| status | varchar(40) |
| subtotal_minor | bigint |
| discount_minor | bigint |
| delivery_fee_minor | bigint |
| service_fee_minor | bigint |
| total_minor | bigint |
| currency | char(3) |
| delivery_address_snapshot | jsonb |
| customer_note | varchar(1000) nullable |
| placed_at | timestamptz |
| confirmed_at | timestamptz nullable |
| delivered_at | timestamptz nullable |
| cancelled_at | timestamptz nullable |
| cancellation_reason | varchar(500) nullable |
| version | bigint |
| created_at | timestamptz |
| updated_at | timestamptz |

Indexes:
- `(customer_id, created_at DESC)`
- `(branch_id, status, created_at DESC)`
- `(status, created_at)`

### `order_items`

`id`, `order_id`, `menu_item_id nullable`, `item_name_snapshot`, `unit_price_minor`, `quantity`, `line_total_minor`, `selected_options jsonb`, timestamps.

Index `(order_id)`.

## 5.6 Promotion tables

### `promotions`

`id`, `code UNIQUE`, `name`, `type`, `value`, `min_order_minor`, `max_discount_minor nullable`, `starts_at`, `ends_at`, `usage_limit nullable`, `usage_count`, `status`, timestamps.

### `promotion_redemptions`

`id`, `promotion_id`, `user_id`, `order_id UNIQUE`, `discount_minor`, `created_at`.

## 5.7 Payment tables

### `payments`

`id`, `order_id`, `method`, `status`, `amount_minor`, `currency`, `provider`, `provider_payment_id nullable`, `idempotency_key UNIQUE`, `initiated_at`, `paid_at nullable`, `failed_at nullable`, `failure_code nullable`, `provider_payload jsonb nullable`, timestamps.

### `payment_events`

`id`, `provider`, `provider_event_id UNIQUE`, `signature_valid`, `payload jsonb`, `received_at`, `processed_at nullable`, `processing_error nullable`.

## 5.8 Delivery/driver tables

### `driver_profiles`

`user_id PK/FK users`, `status`, `license_reference`, `vehicle_type`, `rating numeric(3,2)`, `verified_at nullable`, timestamps.

### `deliveries`

`id`, `order_id UNIQUE`, `status`, `assigned_driver_id nullable`, pickup coordinates, dropoff coordinates, `estimated_dropoff_at nullable`, `picked_up_at nullable`, `delivered_at nullable`, `failed_at nullable`, timestamps.

Indexes `(assigned_driver_id, status)`, `(status, created_at)`.

### `delivery_assignments`

`id`, `delivery_id`, `driver_id`, `status`, `offered_at`, `responded_at nullable`, `accepted_at nullable`, `rejected_at nullable`, timestamps.

Unique index prevents more than one active accepted assignment for a delivery.

### `driver_locations`

`id`, `driver_id`, `delivery_id nullable`, `latitude`, `longitude`, `accuracy_m nullable`, `speed_mps nullable`, `heading_deg nullable`, `recorded_at`.

This table stores sampled history only. Current location is stored in Redis.

## 5.9 Notification/review/audit tables

### `notifications`

`id`, `user_id`, `type`, `title`, `body`, `data jsonb`, `read_at nullable`, `created_at`.

Index `(user_id, read_at, created_at DESC)`.

### `reviews`

`id`, `order_id UNIQUE`, `customer_id`, `restaurant_id`, `driver_id nullable`, `restaurant_rating`, `driver_rating nullable`, `comment nullable`, `status`, timestamps.

### `audit_events`

`id`, `actor_user_id nullable`, `action`, `resource_type`, `resource_id`, `metadata jsonb`, `ip inet nullable`, `created_at`.

Index `(resource_type, resource_id, created_at DESC)` and `(actor_user_id, created_at DESC)`.

## 5.10 Outbox table

### `outbox_messages`

Fields:
- `id uuid PK`
- `aggregate_type varchar(120)`
- `aggregate_id uuid`
- `event_type varchar(160)`
- `payload jsonb`
- `occurred_at timestamptz`
- `published_at timestamptz nullable`
- `attempts int`
- `last_error text nullable`
- `available_at timestamptz`

Index `(published_at, available_at)`.

## 5.11 Referential actions

- User → customer/driver/profile: restrict accidental deletion; use status/deactivation.
- Restaurant → branches: restrict hard deletion once orders exist.
- Order → order items: cascade only if business deletion is allowed; v1 orders are never hard-deleted.
- Payment events are retained for audit.

## 5.12 Required database guarantees

The database must enforce at least:
- unique user email
- unique user phone where present
- unique restaurant slug
- unique order number
- unique payment idempotency key
- unique provider payment event ID
- unique order review
- one active cart per customer/branch
- valid FK relationships
- non-negative monetary fields
- quantity > 0
