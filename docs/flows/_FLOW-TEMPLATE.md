# Flow: <FLOW_NAME>

**Status:** `<...>`

## 1. Goal

`<...>`

## 2. Actors

`<...>`

## 3. Preconditions

`<...>`

## 4. Sequence

```text
Actor
  |
  | action
  v
Client
  |
  | request/event
  v
Backend
  |
  +----> Database
  |
  +----> External service
  |
  +----> Worker/Event
  v
Client / Actor
```

## 5. State transitions

`<...>`

## 6. Authorization checks

`<...>`

## 7. Failure paths

`<...>`

## 8. Retry / idempotency

`<...>`

## 9. Observability

`<Logs, metrics, traces, correlation IDs>`

## 10. Security considerations

`<...>`
