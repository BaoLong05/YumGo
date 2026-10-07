# Authorization Security

**Status:** `<...>`

## 1. Authorization model

Role → Permission → Resource Scope → Action

## 2. Server-side enforcement

`<Policies/guards/handlers/middleware/etc.>`

## 3. Tenant/resource isolation

`<...>`

## 4. Privilege escalation controls

`<...>`

## 5. Frontend rules

UI hiding is not security. Backend authorization remains authoritative.

## 6. Authorization tests

Cover:
- permitted action
- forbidden action
- wrong resource owner
- wrong tenant/scope
- missing permission
- role changes
