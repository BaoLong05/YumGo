# Authorization Model

**Status:** `<...>`

## 1. Model

Recommended model:

```text
Authenticated User
       |
       v
     Role(s)
       |
       v
  Permission(s)
       |
       v
 Resource / Tenant / Branch Scope
       |
       v
     Action
```

## 2. Roles

| Role | Purpose |
|---|---|
| `<...>` | `<...>` |

## 3. Permissions

Use stable names such as:
- `order.view`
- `order.manage`
- `statistics.view`
- `employee.manage`

## 4. Scope

Describe organization/tenant/branch/project/owner scoping.

## 5. Screen permissions

Document UI visibility rules, while keeping backend authorization authoritative.

## 6. Server authorization

Describe policies/middleware/guards/handlers.

## 7. Security invariants

Examples:
- Never trust client-provided role or tenant ID.
- Never authorize only by hidden frontend UI.
- Always validate resource ownership/scope server-side.

## 8. Permission matrix

| Capability | Role A | Role B | Role C |
|---|---:|---:|---:|
| `<...>` | ✓ | — | ✓ |
