# Table Specification

## 1. `users`
| Field | Type | Null | Constraint / Validation | Meaning |
|---|---|---:|---|---|
| id | varchar(21) | NO | PK, NanoID | User identity |
| email | varchar(254) | NO | normalized lowercase, unique | Login identifier |
| phone | varchar(20) | YES | E.164-like normalization when present, unique partial index | Optional phone |
| password_hash | varchar(255) | NO | hash only, never plaintext | Password |
| status | varchar(20) | NO | active/suspended/disabled | Account state |
| email_verified_at | timestamptz | YES | required before privileged flows | Email verification |
| created_at | timestamptz | NO | UTC | Creation |
| updated_at | timestamptz | NO | UTC | Update |
| deleted_at | timestamptz | YES | soft delete | Logical deletion |

Cross-field: disabled/suspended users cannot create orders, refresh sessions or perform privileged operations.

## 2. `customer_profiles`
| Field | Type | Null | Validation |
| id | varchar(21) | NO | PK NanoID |
| user_id | varchar(21) | NO | FK users, unique |
| full_name | varchar(120) | NO | trimmed, 2..120 chars |
| date_of_birth | date | YES | not future |
| avatar_url | varchar(1000) | YES | http/https only |

## 3. `addresses`
| Field | Type | Null | Validation |
| id | varchar(21) | NO | PK NanoID |
| user_id | varchar(21) | NO | FK users |
| label | varchar(40) | NO | 1..40 |
| recipient_name | varchar(120) | NO | 2..120 |
| recipient_phone | varchar(20) | NO | normalized phone |
| address_line | varchar(255) | NO | 5..255 |
| ward | varchar(100) | NO | nonblank |
| district | varchar(100) | NO | nonblank |
| city | varchar(100) | NO | nonblank |
| latitude | numeric(9,6) | YES | -90..90 |
| longitude | numeric(9,6) | YES | -180..180 |
| is_default | boolean | NO | default false |
| created_at | timestamptz | NO | UTC |
| updated_at | timestamptz | NO | UTC |

Cross-field: latitude and longitude must be both null or both non-null.

## 4. `restaurants`
| Field | Type | Null | Validation |
| id | varchar(21) | NO | PK NanoID |
| owner_user_id | varchar(21) | NO | FK users |
| name | varchar(160) | NO | 2..160 |
| description | varchar(2000) | YES | 0..2000 |
| status | varchar(20) | NO | pending/active/suspended/closed |
| created_at | timestamptz | NO | UTC |
| updated_at | timestamptz | NO | UTC |
| deleted_at | timestamptz | YES | soft delete |

## 5. `restaurant_branches`
| Field | Type | Null | Validation |
| id | varchar(21) | NO | PK NanoID |
| restaurant_id | varchar(21) | NO | FK restaurants |
| name | varchar(160) | NO | unique within restaurant |
| phone | varchar(20) | NO | normalized |
| address_line | varchar(255) | NO | nonblank |
| ward | varchar(100) | NO | nonblank |
| district | varchar(100) | NO | nonblank |
| city | varchar(100) | NO | nonblank |
| latitude | numeric(9,6) | NO | -90..90 |
| longitude | numeric(9,6) | NO | -180..180 |
| status | varchar(20) | NO | draft/open/closed/suspended |
| opens_at | time | YES | required when schedule is configured |
| closes_at | time | YES | required when schedule is configured |
| created_at | timestamptz | NO | UTC |
| updated_at | timestamptz | NO | UTC |
| deleted_at | timestamptz | YES | soft delete |

Cross-field: `opens_at` and `closes_at` must be both null or both non-null. Overnight schedule must be represented explicitly if supported.

## 6. `restaurant_staff`
`id`, `restaurant_id`, `user_id`, `role`, `status`, `created_at`.
Unique: `(restaurant_id,user_id,role)`.
Rule: a restaurant staff user can only manage branches belonging to that restaurant.

## 7. `driver_profiles`
`id`, `user_id` unique, `vehicle_type`, `license_number`, `status`, `current_latitude`, `current_longitude`, timestamps.
Vehicle type is enum-like check. Coordinates must be all-null or both non-null.

## 8. `driver_availability`
`id`, `driver_id`, `status(offline/online/busy)`, `last_seen_at`, timestamps.
Unique active row per driver.

## 9. `menu_categories`
`id`, `branch_id`, `name`, `display_order`, `status(active/inactive)`, timestamps, `deleted_at`.
Unique `(branch_id, normalized_name)` among non-deleted rows.

## 10. `menu_items`
| Field | Type | Rule |
|---|---|---|
| id | varchar(21) | PK NanoID |
| branch_id | varchar(21) | FK branch |
| category_id | varchar(21) | FK category |
| name | varchar(160) | required, 2..160 |
| description | varchar(2000) | optional |
| price | numeric(12,2) | >= 0 |
| stock_status | varchar(20) | available/unavailable/out_of_stock |
| status | varchar(20) | active/inactive |
| created_at/updated_at | timestamptz | UTC |
| deleted_at | timestamptz | soft delete |

Cross-table: `category.branch_id = menu_items.branch_id`.

## 11. `carts`
`id`, `customer_id`, `branch_id`, `status(active/checked_out/abandoned)`, `expires_at`, timestamps.
Unique active cart per `(customer_id, branch_id)`.

## 12. `cart_items`
`id`, `cart_id`, `menu_item_id`, `quantity`, `unit_price_snapshot`, timestamps.
Validation: quantity 1..99, unit price >= 0. Cross-table: item branch must equal cart branch and menu item must be active/available at mutation time.

## 13. `promotions`
`id`, `restaurant_id nullable`, `code`, `name`, `type(percent/fixed/free_delivery)`, `value`, `min_order_amount`, `max_discount_amount`, `usage_limit`, `per_user_limit`, `starts_at`, `ends_at`, `status`, timestamps.
Cross-field: `ends_at > starts_at`; percent value 0..100; fixed value >=0; max discount only for percent/fixed.

## 14. `orders`
Key fields:
`id`, `order_number` unique human-readable, `customer_id`, `branch_id`, `status`, `subtotal`, `discount_amount`, `delivery_fee`, `tax_amount`, `total_amount`, promotion snapshot fields, address snapshot fields, `placed_at`, `confirmed_at`, `preparing_at`, `ready_at`, `picked_up_at`, `delivered_at`, `cancelled_at`, `cancel_reason`, timestamps.

Checks:
- all money >= 0;
- `total_amount = subtotal - discount_amount + delivery_fee + tax_amount`;
- `discount_amount <= subtotal`;
- timestamps must respect lifecycle order;
- cancelled orders must have `cancelled_at` and reason.

## 15. `order_items`
`id`, `order_id`, `menu_item_id`, `item_name_snapshot`, `unit_price`, `quantity`, `line_total`.
Check: `line_total = unit_price * quantity`.

## 16. `payments`
`id`, `order_id` unique when one active payment is allowed, `method`, `status`, `amount`, `provider`, `provider_payment_id`, `paid_at`, timestamps.
Cross-field: payment amount must equal order total unless the method supports partial/adjustment explicitly.

## 17. `payment_transactions`
`id`, `payment_id`, `provider_transaction_id`, `transaction_type`, `amount`, `status`, `raw_reference`, `created_at`.
Unique provider transaction id.

## 18. `refunds`
`id`, `payment_id`, `order_id`, `amount`, `reason`, `status`, `provider_ref`, timestamps.
Check: refund amount > 0 and cumulative successful refunds <= successful paid amount.

## 19. `deliveries`
`id`, `order_id` unique, `status(pending/assigned/picked_up/delivering/delivered/failed/cancelled)`, pickup coordinates, dropoff coordinates, timestamps.
Cross-table: dropoff matches order snapshot coordinates when available.

## 20. `delivery_assignments`
`id`, `delivery_id`, `driver_id`, `status(offered/accepted/rejected/cancelled/completed)`, `offered_at`, `responded_at`, timestamps.
Only one active accepted assignment per delivery.

## 21. `driver_locations`
`id`, `driver_id`, `delivery_id nullable`, `latitude`, `longitude`, `recorded_at`.
Used for justified tracking history; current location may also be cached in Redis.

## 22. `reviews`
`id`, `order_id`, `customer_id`, `restaurant_id`, `rating`, `comment`, `status(visible/hidden)`, timestamps.
Unique `(order_id,customer_id)`.
Business rule: only delivered/completed orders may be reviewed.

## 23. `devices`
`id`, `user_id`, `platform`, `push_token`, `status`, `last_seen_at`, unique active token.

## 24. `notifications`
`id`, `user_id`, `type`, `title`, `body`, `data_json`, `read_at`, `created_at`.
No sensitive secrets in `data_json`.

## 25. `roles`, `permissions`, `user_roles`, `role_permissions`
All IDs NanoID. Unique role name, unique permission code, unique join pairs.

## 26. `refresh_tokens`
`id`, `user_id`, `token_hash`, `expires_at`, `revoked_at`, `replaced_by_id`, timestamps.
Store hash, never plaintext refresh token.

## 27. `idempotency_keys`
`id`, `user_id`, `scope`, `idempotency_key`, `request_hash`, `response_status`, `response_body`, `expires_at`, timestamps.
Unique `(user_id,scope,idempotency_key)`.

## 28. `outbox_messages`
`id`, `aggregate_type`, `aggregate_id`, `event_type`, `payload_json`, `occurred_at`, `published_at`, `retry_count`, `last_error`.
Used for reliable async publication.

## 29. `audit_logs`
`id`, `actor_user_id nullable`, `action`, `resource_type`, `resource_id`, `before_json`, `after_json`, `ip_address`, `user_agent`, `created_at`.
Do not store passwords/tokens.
