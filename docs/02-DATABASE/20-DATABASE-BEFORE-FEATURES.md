# Build Order — Database first

Do NOT begin with `RegisterController`, `OrderService`, `CartController`, etc.

## Phase 1 — Database foundation
1. PostgreSQL docker container.
2. Create initial schema migration.
3. Implement NanoID type/convention.
4. Create users/roles/permissions.
5. Create restaurant/branch/staff.
6. Create menu/category/item.
7. Create addresses/cart/cart_items.
8. Create orders/order_items.
9. Create payments/transactions/refunds.
10. Create deliveries/assignments/driver_locations.
11. Create notifications/devices.
12. Create idempotency/outbox/audit.
13. Add all FK/unique/check constraints.
14. Add required indexes.
15. Seed reference roles/permissions only.

## Phase 2 — Integrity test
Before business features:
- insert invalid FK -> must fail;
- duplicate unique value -> must fail;
- invalid money/range -> must fail;
- invalid status -> must fail;
- invalid branch/category/item relation -> application test must fail;
- transaction rollback test -> no partial order.

## Phase 3 — Feature implementation
Only after database and integrity tests pass, implement features in order:
`Auth → Profile/Address → Restaurant → Menu → Discovery → Cart → Promotions → Checkout → Place Order → Restaurant Processing → Driver → Delivery → Payment → Cancel/Refund → Review → Notification → Admin/Operations`.

This order prevents building feature logic on an unstable data model.
