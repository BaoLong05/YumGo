# Relationship Matrix — must stay consistent

| Parent | Child | Cardinality | FK | Delete rule | Extra validation |
|---|---|---|---|---|---|
| users | customer_profiles | 1:0..1 | customer_profiles.user_id | RESTRICT | one profile only |
| users | addresses | 1:N | addresses.user_id | RESTRICT | user owns address |
| users | restaurants | 1:N | restaurants.owner_user_id | RESTRICT | owner must be authorized |
| restaurants | restaurant_branches | 1:N | restaurant_branches.restaurant_id | RESTRICT | branch belongs to restaurant |
| restaurants | restaurant_staff | 1:N | restaurant_staff.restaurant_id | RESTRICT | staff scope |
| users | restaurant_staff | 1:N | restaurant_staff.user_id | RESTRICT | no duplicate assignment |
| restaurant_branches | menu_categories | 1:N | menu_categories.branch_id | RESTRICT | same branch scope |
| menu_categories | menu_items | 1:N | menu_items.category_id | RESTRICT | category.branch_id=item.branch_id |
| restaurant_branches | menu_items | 1:N | menu_items.branch_id | RESTRICT | source branch |
| users | carts | 1:N | carts.customer_id | RESTRICT | one active cart per branch |
| restaurant_branches | carts | 1:N | carts.branch_id | RESTRICT | cart source |
| carts | cart_items | 1:N | cart_items.cart_id | CASCADE | item belongs to cart |
| menu_items | cart_items | 1:N | cart_items.menu_item_id | RESTRICT | item.branch_id=cart.branch_id |
| users | orders | 1:N | orders.customer_id | RESTRICT | customer owns order |
| restaurant_branches | orders | 1:N | orders.branch_id | RESTRICT | order source |
| orders | order_items | 1:N | order_items.order_id | RESTRICT | at least one item at placement |
| menu_items | order_items | 1:N | order_items.menu_item_id | RESTRICT | snapshot protects history |
| orders | payments | 1:N/1 active | payments.order_id | RESTRICT | amount/status consistency |
| payments | payment_transactions | 1:N | payment_transactions.payment_id | RESTRICT | provider transaction unique |
| orders | deliveries | 1:0..1 | deliveries.order_id | RESTRICT | only placed order |
| deliveries | delivery_assignments | 1:N | delivery_assignments.delivery_id | RESTRICT | one active accepted driver |
| driver_profiles | delivery_assignments | 1:N | driver_id | RESTRICT | only eligible driver |
| deliveries | driver_locations | 1:N | delivery_id | RESTRICT | tracking scope |
| orders | reviews | 1:0..1/customer | order_id | RESTRICT | delivered only |
| users | reviews | 1:N | customer_id | RESTRICT | reviewer is order customer |
| restaurants | reviews | 1:N | restaurant_id | RESTRICT | order branch belongs restaurant |
| users | notifications | 1:N | user_id | RESTRICT | recipient only |
| users | devices | 1:N | user_id | RESTRICT | token ownership |

### Cross-relation invariants
1. `menu_item.category_id` and `menu_item.branch_id` must point to the same branch.
2. `cart_item.menu_item_id` must belong to `cart.branch_id`.
3. `order.branch_id` determines the restaurant that is fulfilling the order.
4. `order_item.menu_item_id` must belong to `order.branch_id` at placement time.
5. `delivery.order_id` is one-to-one and cannot point to a cancelled-before-placement order.
6. Driver may accept assignment only if driver status is online/available and assignment is still offered.
7. Review customer must equal order customer and order must be delivered.
8. Payment success cannot be set by client-provided state; only verified server callback or trusted provider response may transition it.
