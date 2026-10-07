# YumGo — Complete System Documentation

Version: 2.0
Status: Approved implementation baseline
Scope: Product requirements + domain + database + API + security + realtime + operations

## 1. Purpose

YumGo is a production-style food ordering and delivery platform used as the main .NET portfolio project. The system is designed as a modular monolith first, with clear module boundaries so individual modules can later become services without rewriting business rules.

## 2. Final product surfaces

### Web — `web/yumgo-web`

One Next.js application with role-aware routes for:
- Customer
- Restaurant staff/manager
- Operations
- Administrator

### Mobile — `mobile/yumgo-mobile`

Exactly one React Native + Expo application. The authenticated role determines whether the user sees customer or driver workflows. There is no separate customer/driver codebase.

### Backend — `backend/`

ASP.NET Core API using Clean Architecture + DDD + PostgreSQL + Redis + SignalR. Durable asynchronous work uses the outbox pattern and a message broker adapter.

## 3. Primary business outcome

A customer can discover a restaurant, browse a branch menu, build a cart, place an order, pay or select cash-on-delivery, follow order state, see the assigned driver's live location, receive notifications, and review the completed order.

Restaurant staff can manage menus and accept/prepare orders.

Drivers can go online, receive eligible delivery offers, accept a delivery, report location, pick up an order, complete delivery, and become available again.

Operations can monitor orders, deliveries, payments, drivers, restaurants and system alerts.

## 4. Non-goals for v1

The following are intentionally outside the first production slice:
- Multi-country currencies
- Loyalty points
- Subscription plans
- Multi-restaurant checkout in one order
- Split payments across multiple cards
- Auction-style driver bidding
- User-created restaurants
- Full event sourcing
- Microservices deployment

These can be added later without changing the core order aggregate contract.

## 5. Architecture rule

```text
HTTP / Web / Mobile
        ↓
      API
        ↓
   Application
        ↓
      Domain
        ↑
 Infrastructure ── PostgreSQL / Redis / broker / providers
```

The dependency rule is compile-time enforced:
- Domain references no Infrastructure or API.
- Application references Domain.
- Infrastructure references Application + Domain.
- API references Application and the composition-root infrastructure package.

## 6. Documentation rule

This folder is the source of truth for intended behavior. A feature is not considered implemented merely because a route or table exists. Implementation must satisfy the workflows, invariants, authorization rules, error contract, tests and Definition of Done in this documentation set.
