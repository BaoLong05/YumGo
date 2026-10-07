# Realtime Architecture

**Status:** `<...>`

## 1. Why realtime is needed

`<...>`

## 2. Protocol

`<SignalR/WebSocket/SSE/MQTT/etc.>`

## 3. Flow

```text
Producer
   |
   v
Backend / Broker
   |
   v
Realtime Channel
   |
   +----> Web
   |
   +----> Mobile
```

## 4. Authentication / authorization

`<...>`

## 5. Event/message contract

| Event | Producer | Consumers | Payload |
|---|---|---|---|
| `<...>` | `<...>` | `<...>` | `<...>` |

## 6. Ordering / duplicates

`<...>`

## 7. Reconnect

`<...>`

## 8. Backpressure / rate limiting

`<...>`

## 9. Source of truth

Realtime delivery is not the authoritative store for important business state.
