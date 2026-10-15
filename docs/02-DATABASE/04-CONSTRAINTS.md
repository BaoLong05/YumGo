# PostgreSQL Constraints and field validation coverage

## 1. Field-level constraint classes
Every field must be evaluated for:
- required/optional;
- type and range;
- length;
- format;
- normalization;
- uniqueness;
- FK integrity;
- status compatibility;
- temporal consistency;
- authorization scope.

## 2. Required database checks
Examples:
```sql
CHECK (price >= 0)
CHECK (quantity BETWEEN 1 AND 99)
CHECK (rating BETWEEN 1 AND 5)
CHECK (starts_at IS NULL OR ends_at IS NULL OR ends_at > starts_at)
CHECK (latitude BETWEEN -90 AND 90)
CHECK (longitude BETWEEN -180 AND 180)
```

## 3. Partial unique indexes
Use partial unique indexes for stateful rules, e.g. one default address per user or one active cart per branch.

## 4. Cross-field rules that DB alone cannot fully express
These MUST be enforced in application/domain transaction:
- order total equation;
- cart item branch consistency;
- payment amount = order payable amount;
- cumulative refunds <= payment amount;
- driver assignment lifecycle;
- review only after delivery;
- restaurant status and branch status compatibility;
- promotion eligibility;
- ownership and permission scope.

## 5. Transaction boundary
Operations that create or transition multiple related rows must run in one transaction when partial completion would create invalid business state.

Examples:
- place order + order items + payment pending + cart checked_out;
- accept driver assignment + delivery assigned;
- payment success + order payment state transition;
- refund create + payment remaining refundable amount calculation.
