# Validation Coverage Matrix

## 1. Validation layers

### Layer A — Request / DTO
Reject malformed input before business logic:
- type;
- required fields;
- max length;
- format;
- basic numeric range;
- unknown/extra fields where strict binding is desired.

### Layer B — Application / Domain
Enforce business context:
- owner/scope;
- state machine;
- cross-field arithmetic;
- cross-entity consistency;
- permissions;
- current database state;
- concurrency.

### Layer C — PostgreSQL
Guarantee structural integrity:
- PK;
- FK;
- unique;
- check;
- not-null;
- partial unique index.

No critical rule should rely only on Layer A.

## 2. Coverage checklist per mutation
For every create/update command answer all of these:
1. Are all required fields present?
2. Are fields normalized?
3. Are lengths/ranges valid?
4. Is the record owned by the actor?
5. Does every referenced ID exist?
6. Do referenced records belong to the same business scope?
7. Are referenced records in an allowed status?
8. Are date ranges valid?
9. Are money values non-negative and internally consistent?
10. Is the operation allowed in the current state?
11. Can concurrent requests create duplicates?
12. Is idempotency required?
13. What DB transaction wraps the change?
14. What audit entry is needed?
15. What event/notification/job is created?

## 3. Examples

### Add cart item
- menu item exists;
- menu item is active;
- menu item is available;
- menu item.branch_id == cart.branch_id;
- cart belongs to current customer;
- cart is active;
- quantity 1..99;
- price is read from DB, never trusted from client;
- duplicate item increments or replaces quantity according to defined rule;
- concurrent updates use transaction/atomic strategy.

### Place order
- customer is active;
- address belongs to customer;
- cart belongs to customer;
- cart has at least one item;
- all cart items are still available;
- branch can accept orders;
- promotion is still valid;
- all prices are reread from DB;
- totals are recalculated server-side;
- address snapshot is copied;
- order and order items are inserted atomically;
- cart becomes checked_out only after order succeeds.

### Payment webhook
- signature valid;
- provider event ID unique/idempotent;
- payment exists;
- amount/currency match expected values;
- state transition allowed;
- duplicate webhook produces same final result.

### Review
- authenticated customer;
- order belongs to customer;
- order is delivered;
- review does not already exist for that order/customer;
- rating 1..5;
- comment <= defined limit;
- restaurant in order is target restaurant.
