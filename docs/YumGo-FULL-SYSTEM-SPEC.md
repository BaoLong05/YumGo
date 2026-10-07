# YumGo — Full System Specification

This is the consolidated entry point for the complete YumGo system specification. Every referenced document below contains actual requirements and concrete rules; none is a placeholder.

## Document index

- [00-README.md](./00-README.md)
- [01-PRODUCT-SCOPE.md](./01-PRODUCT-SCOPE.md)
- [02-ACTORS-PERMISSIONS.md](./02-ACTORS-PERMISSIONS.md)
- [03-ARCHITECTURE.md](./03-ARCHITECTURE.md)
- [04-DOMAIN-MODEL.md](./04-DOMAIN-MODEL.md)
- [05-DATABASE.md](./05-DATABASE.md)
- [06-API.md](./06-API.md)
- [07-ORDER-FLOWS.md](./07-ORDER-FLOWS.md)
- [08-PAYMENT.md](./08-PAYMENT.md)
- [09-REALTIME-DELIVERY.md](./09-REALTIME-DELIVERY.md)
- [10-AUTH-SECURITY.md](./10-AUTH-SECURITY.md)
- [11-NOTIFICATIONS-JOBS.md](./11-NOTIFICATIONS-JOBS.md)
- [12-CACHE-PERFORMANCE.md](./12-CACHE-PERFORMANCE.md)
- [13-TESTING.md](./13-TESTING.md)
- [14-OBSERVABILITY.md](./14-OBSERVABILITY.md)
- [15-UI-ROUTES.md](./15-UI-ROUTES.md)
- [16-ERROR-IDEMPOTENCY-CONCURRENCY.md](./16-ERROR-IDEMPOTENCY-CONCURRENCY.md)
- [17-DEPLOYMENT.md](./17-DEPLOYMENT.md)
- [18-IMPLEMENTATION-PLAN.md](./18-IMPLEMENTATION-PLAN.md)
- [19-DEFINITION-OF-DONE.md](./19-DEFINITION-OF-DONE.md)
- [20-ADR.md](./20-ADR.md)
- [21-CONTRACT-EXAMPLES.md](./21-CONTRACT-EXAMPLES.md)
- [22-COMPLETENESS-CHECK.md](./22-COMPLETENESS-CHECK.md)

## Canonical architecture summary

```text
                    ┌──────────────────────────┐
                    │ Next.js Web Application  │
                    │ customer / restaurant /  │
                    │ operations / admin      │
                    └────────────┬─────────────┘
                                 │ HTTPS
                    ┌────────────▼─────────────┐
                    │ ASP.NET Core API         │
                    │ Auth / API / SignalR     │
                    └───────┬────────┬─────────┘
                            │        │
                 ┌──────────▼──┐  ┌─▼──────────────┐
                 │ Application │  │ Infrastructure │
                 │ + Domain    │  │ EF/Redis/      │
                 │ modules     │  │ Broker/Provider│
                 └─────────────┘  └──────┬─────────┘
                                        │
                           ┌────────────▼────────────┐
                           │ PostgreSQL   Redis      │
                           │ source truth current   │
                           │ business     location  │
                           └────────────────────────┘

React Native + Expo mobile app
  ├─ Customer experience
  └─ Driver experience
  selected by authenticated role and scope
```

## Non-negotiable implementation rules

1. Business rules live in domain/application, not controllers.
2. PostgreSQL is authoritative; Redis is not the permanent order/payment store.
3. Client totals and client payment-success claims are never trusted.
4. Critical writes are idempotent.
5. Concurrent state transitions are protected by database constraints/optimistic concurrency.
6. Outbox events are committed with the business transaction.
7. External providers are accessed through application ports/adapters.
8. Resource authorization is enforced server-side.
9. Async I/O flows through `CancellationToken`; blocking waits are forbidden in request handling.
10. The first deployment is modular-monolith; microservices are an evolution, not a prerequisite.
