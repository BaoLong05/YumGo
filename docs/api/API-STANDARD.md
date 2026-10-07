# API Documentation Standard

## Required information for every endpoint / operation

- Purpose
- Method / operation
- Path or operation name
- Authentication
- Permission
- Resource scope
- Request headers
- Path/query parameters
- Request body
- Validation rules
- Success response
- Error responses
- Idempotency behavior
- Pagination/filter/sort behavior where relevant
- Rate limits where relevant
- Side effects
- Compatibility/versioning notes

## Example

### POST `/api/v1/<resource>`

**Purpose:** `<...>`

**Authentication:** Required

**Permission:** `<resource.create>`

**Scope:** `<tenant/org/owner/etc.>`

**Request:**
```json
{}
```

**Success:** `201 Created`

```json
{}
```

**Errors:**
- `400` — invalid request
- `401` — unauthenticated
- `403` — forbidden
- `409` — business conflict
- `422` — semantically invalid data, where used by the project

**Idempotency:** `<...>`

**Side effects:** `<...>`
