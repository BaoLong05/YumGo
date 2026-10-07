# 14. Observability and Operations

## 14.1 Logging

Use structured `ILogger<T>` logs.

Useful fields:
- correlation ID
- request ID
- user ID where appropriate
- order ID
- delivery ID
- driver ID
- branch ID
- operation name
- outcome
- elapsed time

Never log:
- passwords
- access tokens
- refresh tokens
- payment secrets
- full private addresses unless operationally required

## 14.2 Correlation

Every request has a correlation identifier. The identifier is propagated to:
- application logs
- outgoing HTTP provider calls where supported
- background messages
- outbox event metadata

## 14.3 Metrics

HTTP:
- request count
- error count
- latency p50/p95/p99

Business:
- orders placed
- orders confirmed
- orders cancelled
- payment success/failure
- active deliveries
- average delivery time
- driver acceptance rate

Infrastructure:
- PostgreSQL query latency
- connection pool saturation
- Redis latency/errors
- broker queue depth
- outbox backlog
- failed job count

## 14.4 Distributed tracing

Trace across:

```text
HTTP request
 → application use case
 → EF Core / PostgreSQL
 → Redis
 → external provider
 → background message
```

Use OpenTelemetry-compatible instrumentation.

## 14.5 Health endpoints

- `/health/live` — process is alive
- `/health/ready` — required dependencies are reachable

Readiness must not report healthy if required database connectivity is unavailable.

## 14.6 Alerts

Trigger operational alerts for:
- payment webhook verification failures
- payment reconciliation mismatches
- persistent outbox backlog
- broker consumer failures
- database connectivity failures
- high API error rate
- excessive delivery allocation failures

## 14.7 Error investigation flow

```text
Alert
 ↓
Correlation ID / resource ID
 ↓
Request logs
 ↓
Trace
 ↓
Database / Redis / provider metrics
 ↓
Business state inspection
 ↓
Safe remediation
 ↓
Audit event
```
