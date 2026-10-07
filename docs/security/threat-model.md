# Threat Model

**Status:** `<...>`

## 1. Assets

- Accounts
- Credentials/tokens
- Business data
- Payments
- Personal/sensitive data
- Uploaded files
- Infrastructure credentials

## 2. Trust boundaries

```text
Untrusted Client
      |
      v
Public API
      |
      v
Application
      |
      v
Trusted internal resources
```

## 3. Threats

Consider:
- Broken access control
- Credential attacks
- Injection
- XSS/CSRF where applicable
- SSRF
- Path traversal
- Malicious uploads
- Replay/duplicate requests
- Webhook spoofing
- Rate-limit abuse
- Data leakage
- Dependency compromise

## 4. Mitigations

| Threat | Mitigation | Test |
|---|---|---|
| `<...>` | `<...>` | `<...>` |

## 5. Residual risk

`<...>`
