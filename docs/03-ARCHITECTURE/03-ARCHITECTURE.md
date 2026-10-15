# 3. System Architecture

## 3.1 Architectural style

YumGo starts as a modular monolith. Modules are separated by namespace, application feature boundaries and domain aggregates. They share one PostgreSQL deployment but do not share domain objects directly across module boundaries.

The system can later extract modules into services because external communication contracts are already explicit.

## 3.2 Backend layers

### `YumGo.Domain`

Contains:
- entities
- aggregate roots
- value objects
- domain services
- domain events
- domain exceptions
- business invariants

Must not contain:
- EF Core `DbContext`
- HTTP types
- Redis clients
- controller models
- payment SDK types
- framework-specific integration code

### `YumGo.Application`

Contains:
- commands/queries/use cases
- application DTOs
- repository ports
- external-service ports
- authorization checks that require application resource scope
- transaction orchestration
- mapping between transport/application/domain representations

### `YumGo.Infrastructure`

Contains:
- EF Core context/configurations
- repository implementations
- PostgreSQL mappings
- Redis adapters
- SignalR infrastructure integration
- outbox publisher
- payment provider adapters
- map provider adapters
- notification adapters
- durable queue/broker adapters

### `YumGo.Api`

Contains:
- controllers
- API request/response models
- authentication configuration
- middleware
- OpenAPI configuration
- dependency-registration composition root
- health endpoints

## 3.3 Module structure

```text
Identity
Customer
Restaurant
Menu
Cart
Order
Payment
Delivery
Driver
Promotion
Notification
Review
Operations
```

Each module follows a local shape such as:

```text
Application/Features/Orders/
  Commands/
  Queries/
  Dtos/

Domain/Orders/
  Order.cs
  OrderItem.cs
  OrderStatus.cs
  Events/

Infrastructure/Orders/
  OrderRepository.cs
  OrderConfiguration.cs
```

## 3.4 Request flow

```text
HTTP
  ↓
Middleware
  ↓
Authentication
  ↓
Authorization
  ↓
Controller
  ↓
Application use case
  ↓
Domain aggregate / rules
  ↓
Repository / external ports
  ↓
Infrastructure adapter
  ↓
PostgreSQL / Redis / provider
  ↓
Outbox event when required
  ↓
Notification / realtime / async consumer
```

## 3.5 Composition rule

Business features must be expressed as use cases. Controllers should not contain order pricing, state transitions, payment reconciliation or delivery assignment algorithms.

## 3.6 Transaction boundary

The normal write path is one application use case inside one database transaction when multiple relational changes must be atomic.

The outbox row is written in the same transaction as the business state change. Publishing to the broker is separate and retryable.

## 3.7 External provider abstraction

External services are represented by ports:

```csharp
public interface IPaymentProvider
{
    Task<PaymentIntentResult> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken ct);
}

public interface IMapService
{
    Task<RouteResult> CalculateRouteAsync(
        GeoCoordinate origin,
        GeoCoordinate destination,
        CancellationToken ct);
}
```

The domain never depends on a concrete provider.

## 3.8 Realtime architecture

```text
Driver device
   ↓
POST/SignalR location ingestion
   ↓
Authorization + validation
   ↓
Redis current location
   ↓
SignalR group for order
   ↓
Customer device/web
```

Durable business state stays in PostgreSQL. Redis contains fast-changing ephemeral state.
