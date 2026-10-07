# Feature Matrix

| Feature | Primary actor | Main tables | High-risk validation |
|---|---|---|---|
| Register | Customer | users, customer_profiles | normalized unique identity |
| Login | All | users, refresh_tokens | status + credentials |
| Profile | Customer | users, customer_profiles | own-resource scope |
| Address | Customer | addresses | ownership + default uniqueness |
| Restaurant | Owner/Admin | restaurants, branches, staff | restaurant scope |
| Menu | Staff | categories, items | branch consistency |
| Discovery | Customer | branches, items | visibility/status |
| Cart | Customer | carts, cart_items | branch consistency |
| Promotion | Staff/Admin | promotions | window/limits/scope |
| Checkout | Customer | cart, address, promotion | live revalidation |
| Place Order | Customer | orders, items, payments, cart | transaction + idempotency |
| Restaurant Processing | Staff | orders | state machine + scope |
| Driver Assignment | Driver/Ops | driver, delivery, assignment | race/concurrency |
| Delivery | Driver | deliveries, assignments | state transitions |
| Tracking | Driver/Customer | locations + Redis | scope + coordinate validation |
| Payment | Provider/Customer | payments, transactions | signature + amount |
| Cancel/Refund | Customer/Ops | orders, refunds | policy + sum limits |
| Review | Customer | reviews | delivered + ownership |
| Notifications | System | notifications, devices | recipient scope |
| Operations | Ops/Admin | read models / base tables | policy + data minimization |
| User admin | Admin | roles, permissions, user_roles | privilege escalation |
| Audit | System/Admin | audit_logs | append-only behavior |
