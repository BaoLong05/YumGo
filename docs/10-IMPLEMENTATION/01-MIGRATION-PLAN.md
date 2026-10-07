# PostgreSQL Migration Plan

## Baseline status

`20261007090000_FullCommerceSchema` is the consolidated initial schema migration, following the existing incremental user migrations. It creates the remaining 31 tables; together with `users`, the baseline contains 32 tables. `20261007093504_AlignUserUniquenessWithDatabaseSpec` brings the user email/phone indexes in line with the canonical schema. The logical groupings below remain the ownership/order guide for future schema evolution, not separate migrations already present in the repository. Apply with `dotnet ef database update` after PostgreSQL is available.

## Migration 001
Identity and access:
users, roles, permissions, user_roles, refresh_tokens.

## Migration 002
Restaurant core:
restaurants, branches, restaurant_staff.

## Migration 003
Menu:
menu_categories, menu_items.

## Migration 004
Customer commerce:
addresses, carts, cart_items, promotions.

## Migration 005
Orders and money:
orders, order_items, payments, payment_transactions, refunds.

## Migration 006
Delivery:
driver_profiles, driver_availability, deliveries, delivery_assignments, driver_locations.

## Migration 007
Messaging and audit:
devices, notifications, outbox_messages, idempotency_keys, audit_logs.

Every migration must contain:
- forward change;
- index review;
- FK review;
- rollback strategy where applicable;
- seed impact review.
