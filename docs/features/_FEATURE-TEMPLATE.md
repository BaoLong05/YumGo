# Feature: <FEATURE_NAME>

**Status:** `<Implemented / Partially implemented / Planned / Deprecated / Removed>`
**Owner:** `<TEAM/PERSON>`
**Last updated:** `<YYYY-MM-DD>`

## 1. Overview

What problem does this feature solve?

## 2. Actors

| Actor | Role in feature |
|---|---|
| `<...>` | `<...>` |

## 3. User flow

```text
Trigger
  ↓
Step 1
  ↓
Step 2
  ↓
Outcome
```

## 4. Business rules

1. `<Rule>`
2. `<Rule>`
3. `<Rule>`

## 5. Preconditions

`<What must be true before the operation?>`

## 6. State machine / lifecycle

```text
State A
  ↓
State B
  ↓
State C
```

Valid transitions and invalid transitions must be explicit.

## 7. Domain model

### Entities
`<...>`

### Value Objects
`<...>`

### Aggregate / consistency boundary
`<...>`

### Domain services/events
`<...>`

## 8. Use cases

| Use case | Actor | Permission | Result |
|---|---|---|---|
| `<...>` | `<...>` | `<...>` | `<...>` |

## 9. Authorization

### Screen permissions
`<...>`

### Action permissions
`<...>`

### Resource scope
`<tenant/org/branch/owner/etc.>`

### Server enforcement
`<...>`

## 10. API

| Method | Endpoint | Auth | Permission |
|---|---|---|---|
| `<GET/POST/...>` | `<...>` | `<...>` | `<...>` |

### Request

```json
{}
```

### Response

```json
{}
```

### Errors

| Condition | Status | Error code |
|---|---:|---|
| `<...>` | `<...>` | `<...>` |

### Idempotency
`<Required / Not required / Strategy>`

## 11. Persistence

### Tables/entities
`<...>`

### Important constraints
`<...>`

### Transaction boundary
`<...>`

## 12. Integrations

| Integration | Purpose | Failure behavior |
|---|---|---|
| `<...>` | `<...>` | `<...>` |

## 13. Async/background work

`<Jobs/events/queues>`

## 14. Realtime behavior

`<Events pushed to clients, reconnect behavior>`

## 15. Frontend

`<Relevant pages/screens/components and state management>`

## 16. Mobile

`<Relevant screens, permissions, offline behavior>`

## 17. Error handling

`<Expected failure modes and user-facing behavior>`

## 18. Security

`<Sensitive data, trust boundaries, abuse controls, rate limiting, upload/payment considerations>`

## 19. Testing

### Unit
`<...>`

### Integration/API
`<...>`

### E2E
`<...>`

## 20. Edge cases

- `<Case>`
- `<Case>`

## 21. Observability

Logs, metrics, traces, alerts, correlation IDs.

## 22. Known limitations

`<...>`

## 23. Future improvements

`<Clearly labeled planned work>`

## 24. Related documentation

- `<API document>`
- `<Architecture document>`
- `<ADR>`
