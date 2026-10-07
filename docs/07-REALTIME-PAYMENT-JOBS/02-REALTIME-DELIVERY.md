# 9. Realtime and Driver Tracking

## 9.1 Transport

Use SignalR for application-level realtime updates.

Required groups:
- `order:{orderId}` — customer/authorized observers
- `branch:{branchId}` — restaurant order board
- `ops:delivery` — operations dashboard

## 9.2 SignalR events

Server → customer:
- `order.status.changed`
- `delivery.assigned`
- `delivery.location.updated`
- `order.delivered`

Server → restaurant:
- `order.created`
- `order.cancelled`
- `payment.updated`

Server → driver:
- `delivery.offer.created`
- `delivery.offer.expired`
- `delivery.cancelled`

## 9.3 Group authorization

A client must pass an application authorization check before joining a group.

Examples:
- customer may join `order:{id}` only when they own the order
- driver may receive delivery messages only for their own current assignments
- restaurant staff may join the branch group only for an assigned branch
- operations can join operational groups according to permission

## 9.4 Driver location payload

```json
{
  "deliveryId": "NanoID(21)",
  "latitude": 10.123456,
  "longitude": 106.123456,
  "accuracyM": 9.2,
  "speedMps": 5.1,
  "headingDeg": 80,
  "recordedAt": "2026-10-07T06:00:00Z"
}
```

## 9.5 Redis keys

Current location:

`driver:{driverId}:location`

Active delivery:

`driver:{driverId}:active-delivery`

Order tracking state:

`order:{orderId}:tracking`

Keys have TTLs appropriate to the lifecycle. A completed/cancelled delivery must not retain a live tracking key indefinitely.

## 9.6 GPS validation

Reject:
- latitude/longitude outside valid bounds
- impossible jump speeds above configured safety thresholds unless explicitly flagged as low-accuracy
- updates from a driver without active delivery authorization
- stale timestamps older than the configured tolerance

The purpose is to reject obviously invalid data, not to build a navigation-grade GPS integrity system.

## 9.7 Persistence strategy

Redis holds the current point for fast reads.

PostgreSQL `driver_locations` receives sampled points for history/analytics. The persistence cadence is controlled by the application and may be lower than the incoming device frequency.

## 9.8 Maps

The application uses `IMapService` for:
- route calculation
- distance estimation
- ETA calculation
- optional geocoding

Provider-specific responses are mapped into internal types.
