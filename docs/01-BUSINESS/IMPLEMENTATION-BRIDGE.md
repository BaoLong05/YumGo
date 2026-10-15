# YumGo — Bridge từ Business sang Code

Tài liệu này giúp chuyển một tính năng nghiệp vụ thành code mà không nhảy thẳng vào controller.

## Ví dụ: Place Order

### Business

Customer muốn đặt món từ một branch.

### Preconditions

- authenticated
- cart còn hiệu lực
- branch nhận đơn
- item còn available
- address hợp lệ
- payment method hợp lệ

### Business actions

- validate cart
- recalculate total
- snapshot items/prices
- create order
- create payment
- persist atomically
- publish OrderCreated

### Domain/application concepts

- `Order`
- `OrderItem`
- `Money`
- `AddressSnapshot`
- `PlaceOrderCommand`
- `PlaceOrderHandler` hoặc application service
- `IOrderRepository`
- `IPaymentGateway`

### Infrastructure

- EF Core
- PostgreSQL
- payment provider adapter
- outbox publisher

### API

Ví dụ:

```text
POST /api/v1/orders
```

### Test

- happy path
- sold out item
- branch closed
- invalid address
- duplicate idempotency key
- concurrent creation

### Observability

- correlation ID
- order ID
- customer ID
- elapsed time
- success/failure metric

**Nguyên tắc:** business docs quyết định *hệ thống phải làm gì*; technical docs quyết định *code làm điều đó như thế nào*.
