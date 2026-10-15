# NanoID, URL IDs and Hidden Fields

## 1. Required ID strategy
Every business entity uses NanoID(21) as its database primary key and all child FK values use the same NanoID value.

Examples:
```text
/users/7hY...21chars
/restaurants/3kP...21chars
/branches/9aQ...21chars
/orders/AbC...21chars
```

Never expose:
- auto-increment integer IDs;
- UUIDs alongside public NanoIDs as a second identifier;
- email or phone as resource identity in URLs.

## 2. Why
NanoID prevents predictable sequential enumeration such as `/orders/1`, `/orders/2`, `/orders/3`.

It does **not** replace authorization. A user who obtains another valid NanoID must still receive 403/404 according to resource exposure policy.

## 3. Hidden ID fields
For server-rendered forms or equivalent form state:
```html
<input type="hidden" name="restaurantId" value="...nanoid...">
<input type="hidden" name="branchId" value="...nanoid...">
<input type="hidden" name="menuItemId" value="...nanoid...">
```

Rules:
- hidden field is convenience/state transport only;
- backend must load the referenced record by NanoID;
- backend must verify actor scope and relationships;
- backend must ignore client attempts to change ownership/restaurant/customer IDs when those are server-derived.

## 4. API rule
If an endpoint can derive identity from authenticated context, prefer server-derived identity rather than accepting it from body.

Example:
`POST /api/v1/carts/items` should derive `customer_id` from JWT subject, not from `customerId` supplied by client.

## 5. Route rule
Routes use NanoID, e.g.:
`GET /api/v1/orders/{orderId}`

Before returning data:
```text
1. parse NanoID
2. fetch resource
3. verify authenticated actor
4. verify resource scope
5. only then return data
```

## 6. Never rely on obscurity
NanoID improves non-sequential enumeration resistance. It does not authorize access, hide records from an attacker, or make an API secure by itself.
