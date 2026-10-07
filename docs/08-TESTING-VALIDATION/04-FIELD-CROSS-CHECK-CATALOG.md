# Cross-field and cross-table check catalog

| Check | Source fields | Rule |
|---|---|---|
| Address coordinates | latitude + longitude | both null or both present |
| Branch schedule | opens_at + closes_at | both null or both present; validate overnight semantics if used |
| Promotion window | starts_at + ends_at | end > start |
| Promotion percent | type + value | percent value <= 100 |
| Order total | subtotal, discount, fee, tax, total | equation must hold |
| Order cancellation | status + cancelled_at + reason | cancelled => timestamp + reason |
| Order timestamps | placed/confirmed/preparing/ready/picked_up/delivered | monotonic sequence |
| Cart item | cart.branch_id + menu_item.branch_id | must match |
| Menu item | category.branch_id + item.branch_id | must match |
| Order item | order.branch_id + menu_item.branch_id | must match at placement |
| Payment | payment.order_id + amount | amount equals authorized order amount |
| Refund | refund.amount + paid/refunded sum | total refunds <= paid amount |
| Review | review.customer_id + order.customer_id | must match |
| Review | order.status + review | only delivered orders |
| Delivery | delivery.order_id + order.status | cannot exist before order is placed |
| Assignment | driver.status + assignment.status | accepted only from eligible driver |
| Tracking | driver.assignment + deliveryId | driver can publish only assigned delivery |
