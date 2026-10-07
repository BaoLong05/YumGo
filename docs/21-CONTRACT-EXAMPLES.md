# 21. Contract Examples

## 21.1 Restaurant list

```json
{
  "data": [
    {
      "id": "0d7...",
      "name": "Bếp Nhà",
      "slug": "bep-nha",
      "status": "open",
      "rating": 4.7,
      "distanceKm": 1.8,
      "coverImageUrl": "https://cdn.example/restaurant.jpg"
    }
  ],
  "meta": {
    "nextCursor": "eyJjcmVhdGVkQXQiOi...",
    "hasMore": true
  }
}
```

## 21.2 Order summary

```json
{
  "data": {
    "id": "f4b...",
    "orderNumber": "YG-20261007-001234",
    "status": "Delivering",
    "restaurant": {
      "branchId": "a11...",
      "name": "Bếp Nhà — Thủ Đức"
    },
    "money": {
      "currency": "VND",
      "subtotalMinor": 120000,
      "discountMinor": 10000,
      "deliveryFeeMinor": 15000,
      "serviceFeeMinor": 5000,
      "totalMinor": 130000
    },
    "items": [
      {
        "id": "b22...",
        "name": "Cơm gà",
        "quantity": 2,
        "unitPriceMinor": 60000,
        "lineTotalMinor": 120000
      }
    ],
    "tracking": {
      "driverId": "d33...",
      "etaMinutes": 8
    }
  }
}
```

## 21.3 Order status realtime event

Event name:

`order.status.changed`

Payload:

```json
{
  "orderId": "f4b...",
  "orderNumber": "YG-20261007-001234",
  "from": "PickedUp",
  "to": "Delivering",
  "occurredAt": "2026-10-07T06:18:00Z"
}
```

## 21.4 Tracking update

```json
{
  "deliveryId": "d44...",
  "latitude": 10.845123,
  "longitude": 106.756321,
  "accuracyM": 7.2,
  "speedMps": 4.8,
  "headingDeg": 135,
  "recordedAt": "2026-10-07T06:18:05Z"
}
```

## 21.5 Validation error

```json
{
  "type": "https://api.yumgo.local/problems/validation",
  "title": "Validation failed",
  "status": 400,
  "code": "VALIDATION_ERROR",
  "errors": {
    "quantity": ["Quantity must be between 1 and 99."]
  },
  "traceId": "00-..."
}
```

## 21.6 Conflict error

```json
{
  "type": "https://api.yumgo.local/problems/concurrent-modification",
  "title": "Concurrent modification",
  "status": 409,
  "code": "CONCURRENT_MODIFICATION",
  "detail": "The resource changed after it was read.",
  "traceId": "00-..."
}
```
