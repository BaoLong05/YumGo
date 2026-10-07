# 10. Authentication and Security Specification

## 10.1 Authentication model

- Access token: short-lived JWT for API access.
- Refresh token: long-lived opaque random token stored only as a hash server-side.
- Refresh token rotation is required.
- Token revocation occurs on logout, rotation reuse detection, suspension and security reset.

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

Use a proven password hashing implementation from the .NET ecosystem. Do not implement password hashing primitives manually.

Never log passwords.

## 10.4 JWT claims

Minimum useful claims:
- `sub` user ID
- `jti` token ID
- `iat`
- `exp`
- role/permission claims only when their lifecycle is understood

Resource scope must still be checked server-side. A role claim is not a substitute for a resource ownership check.

## 10.5 Refresh-token rotation

On refresh:
1. hash submitted refresh token
2. look up active token record
3. verify not expired/revoked
4. revoke the old token
5. create a new refresh token
6. issue a new access token
7. audit the rotation

Detected reuse of a revoked refresh token triggers revocation of the affected token family/session according to security policy.

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

Secrets belong in environment/secret storage:
- JWT signing key
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
- login success/failure where policy requires
- role changes
- suspension/reactivation
- payment reconciliation
- operations intervention
- delivery reassignment
- sensitive account changes

## 10.14 Personal data handling

Collect only the personal data needed for the product. Do not expose customer addresses or phone numbers to users who lack an operational need for them.
