# Negative / Security Test Matrix

## Identity
- register duplicate normalized email -> 409
- register duplicate normalized phone -> 409
- wrong password -> auth failure without account enumeration
- suspended account -> blocked
- revoked refresh token -> blocked

## Ownership / ID tampering
- customer requests another order NanoID -> 403/404 by policy
- customer changes hidden `branchId` to another restaurant -> reject
- restaurant staff changes hidden `restaurantId` -> reject
- customer submits another user's `addressId` -> reject
- driver submits another deliveryId -> reject

## Relational integrity
- menu item category from another branch -> reject
- cart item from another branch -> reject
- order item not belonging to order branch -> reject
- review for unrelated restaurant -> reject

## Money
- negative price -> reject
- excessive discount -> reject
- client total differs from server total -> server ignores/recalculates
- payment amount mismatch -> webhook rejected
- cumulative refunds exceed paid -> reject

## State
- delivered -> preparing -> reject
- cancelled -> paid -> reject unless explicit refund state transition
- delivery completed without assigned driver -> reject
- driver accepts expired offer -> reject

## Concurrency
- double place order same idempotency key -> one order
- two different keys simultaneous same cart -> one succeeds; second receives conflict based on cart state
- double driver accept -> one accepted assignment
- duplicate webhook -> one state transition
