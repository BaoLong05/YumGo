# API ID Conventions

## Resource routes
```text
GET    /api/v1/restaurants/{restaurantId}
GET    /api/v1/branches/{branchId}
GET    /api/v1/menu-items/{menuItemId}
GET    /api/v1/orders/{orderId}
GET    /api/v1/deliveries/{deliveryId}
```

All path IDs are NanoID(21).

## Request bodies
Use NanoID strings for references.
Example:
```json
{
  "branchId": "V1StGXR8_Z5jdHi6B-myT",
  "addressId": "mJp9xA2kQ8vP6dL3sY4N5",
  "promotionCode": "WELCOME10"
}
```

The server derives actor identity from authentication context. Client should not send `customerId` when it is derivable from the authenticated principal.

## Hidden forms
When a web form requires resource identity, include hidden NanoID values only as transport state. Never use them as proof of ownership.
