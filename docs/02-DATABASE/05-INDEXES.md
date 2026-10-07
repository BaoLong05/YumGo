# Index strategy

Minimum indexes:
- `users(lower(email))` unique.
- `users(phone)` unique partial when non-null.
- all FK columns.
- `restaurant_branches(restaurant_id,status)`.
- `menu_items(branch_id,status,stock_status)`.
- `menu_categories(branch_id,status)`.
- `carts(customer_id,branch_id,status)`.
- `carts(branch_id)` for the branch FK lookup.
- `cart_items(cart_id)`.
- `orders(customer_id,created_at desc)`.
- `orders(branch_id,status,created_at desc)`.
- `orders(order_number)` unique.
- `orders(promotion_id)` for the optional promotion FK.
- `promotions(restaurant_id)` for restaurant-scoped promotion lookups.
- `payments(order_id,status)`.
- `deliveries(status,created_at)`.
- `delivery_assignments(delivery_id,created_at)` and `refunds(payment_id)` for FK/history lookups.
- `delivery_assignments(driver_id,status,created_at desc)`.
- `driver_locations(delivery_id,recorded_at desc)`.
- `notifications(user_id,read_at,created_at desc)`.
- `reviews(customer_id,created_at desc)` for reviewer history.
- `audit_logs(resource_type,resource_id,created_at desc)`.

Never add an index merely because a column exists. Each index must support a query/filter/sort/FK path.
