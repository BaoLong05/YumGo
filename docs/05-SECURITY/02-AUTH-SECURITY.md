# 10. Authentication and Security Specification

## 10.1 Authentication model

- Access token: HS256 JWT, 15 minutes by default (configurable 1–60 minutes), never beyond the session deadline.
- Refresh token: 256-bit opaque random token; only its SHA-256 hash is stored server-side.
- Session lifetime is an absolute 48 hours from login; refresh rotation never extends it.
- Refresh token rotation is required.
- `sid` binds each access JWT to its current refresh-token row; authenticated requests verify the row and active user status. Logout, refresh rotation, expiry, and suspension invalidate that JWT immediately.

## 10.2 Login flow

```text
POST /auth/login
  ↓
Normalize identifier
  ↓
Load user
  ↓
Verify password hash
  ↓
Check account status
  ↓
Issue access token + refresh token
  ↓
Record login audit event
```

A disabled/suspended account cannot receive a new session.

## 10.3 Password storage

Registration uses ASP.NET Core Identity's versioned `PasswordHasher<T>` implementation; verification accepts successful verification and rehash-needed outcomes. Do not implement password hashing primitives manually. The registration policy requires at least 6 characters and at least 3 of 4 groups: uppercase, lowercase, digit and symbol.

Never log passwords.

## 10.4 JWT claims

Minimum useful claims:
- `sub` user ID
- `jti` token ID
- `iat`
- `exp`
- `sid` current refresh-token row ID
- role claims loaded from the database (never accepted from the client)

Resource scope must still be checked server-side. A role claim is not a substitute for a resource ownership check.

## 10.5 Refresh-token rotation

On refresh:
1. hash submitted refresh token
2. look up active token record
3. verify not expired/revoked
4. atomically revoke the old token and create its replacement
5. preserve the original absolute `expires_at` on the replacement
6. issue an access JWT bound to the replacement row
7. audit the rotation without storing token values

Detected reuse of a revoked refresh token revokes its descendant chain. Concurrent refresh attempts are resolved with a conditional database update; a losing/replayed token cannot keep the successor session active.

## 10.6 Transport security

Production traffic must use HTTPS. Sensitive cookies/tokens must use secure storage appropriate to the client platform.

## 10.7 CORS

Allow only configured frontend origins in production. Do not use wildcard origins with credentialed browser requests.

## 10.8 Rate limiting

Apply stronger controls to:
- login
- register
- refresh
- password reset
- OTP-like verification endpoints if later added
- payment initiation
- webhooks when provider guidance allows it

Registration and login permit 5 requests per minute per observed client IP; refresh permits 10 per minute. These are in-process fixed windows (per API instance). Multi-instance deployments need a shared limiter or gateway policy for global quotas. Behind a reverse proxy, configure trusted forwarded-header handling before relying on per-client-IP partitioning; do not trust arbitrary forwarded headers.

## 10.9 Input validation

Validate at the API boundary:
- required fields
- lengths
- formats
- numeric ranges
- enum values
- pagination bounds

Then enforce domain invariants inside use cases/aggregates.

## 10.10 Over-posting protection

Never bind public request DTOs directly to EF entities.

## 10.11 Secret management

Secrets belong in environment/secret storage. The API requires `Authentication__Jwt__Issuer`, `Authentication__Jwt__Audience`, and `Authentication__Jwt__SigningKey`; the signing key must be base64-encoded and contain at least 32 bytes. `Authentication__Jwt__AccessTokenLifetimeMinutes` defaults to 15 and accepts 1–60. Never commit or log the signing key.

Other secrets belong in environment/secret storage:
- database credentials
- Redis credentials if enabled
- payment provider secret
- map provider secret
- notification provider credentials
- broker credentials

## 10.12 Webhook security

Verify signatures before parsing the event as trusted business data.

## 10.13 Audit events

Audit at least:
- login success/failure, refresh rotation/reuse, and logout; never include passwords or raw tokens
- role changes
- suspension/reactivation
- payment reconciliation
- operations intervention
- delivery reassignment
- sensitive account changes

## 10.14 Personal data handling

Collect only the personal data needed for the product. Do not expose customer addresses or phone numbers to users who lack an operational need for them.
