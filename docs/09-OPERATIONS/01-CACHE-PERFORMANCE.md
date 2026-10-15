# 12. Caching and Performance

## 12.1 Why Redis is used

Redis is used for fast-changing or frequently read data where the source of truth remains elsewhere.

Good candidates:
- current driver location
- active delivery presence
- restaurant/menu cache
- rate-limit counters
- short-lived session/verification data

Redis is not the authoritative store for orders, payments or permanent account history.

## 12.2 Cache-aside strategy

For restaurant/menu reads:

```text
GET
 ↓
Redis hit? ── yes → return
 ↓ no
PostgreSQL
 ↓
populate Redis
 ↓
return
```

Invalidate or version keys when the underlying menu/restaurant data changes.

## 12.3 Suggested cache keys

- `restaurant:{id}`
- `branch:{id}`
- `branch:{id}:menu:v{version}`
- `promotion:{code}`
- `driver:{id}:location`
- `order:{id}:tracking`

## 12.4 Cache safety

Do not cache authorization decisions longer than their policy permits.

Never cache secrets or raw credentials.

## 12.5 N+1 prevention

Read APIs should use DTO projection by default.

Examples:
- restaurant list projects only required summary fields
- order history projects order + item summaries without loading unrelated navigation graphs
- dashboard queries use purpose-built projections/aggregations

`Include` is used when loading an entity graph is genuinely required, not as a generic fix for every relation.

## 12.6 Pagination

All high-volume APIs are paginated.

Limits are server-controlled:
- default 20
- maximum 50 for public collections
- stricter limits for expensive operational queries

## 12.7 Database indexing priorities

At minimum:
- unique normalized login identifiers
- order list by customer/status/time
- restaurant branch/status
- menu/category ordering
- delivery driver/status
- notifications by user/read/time
- payment/provider event uniqueness

Indexes are added only when query patterns justify them; each index adds write cost.

## 12.8 Async I/O

Database, Redis and HTTP provider calls use asynchronous APIs and accept `CancellationToken`.

Do not wrap ordinary asynchronous I/O in `Task.Run`.

## 12.9 EF Core rules

- use `AsNoTracking()` for read-only projections where appropriate
- keep `IQueryable` until the query is ready to execute
- never use one `DbContext` concurrently for multiple operations
- inspect generated SQL for complex queries
- use transactions for multi-write consistency
