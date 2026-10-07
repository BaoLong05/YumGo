# Deployment Architecture

**Status:** `<...>`

## 1. Environments

| Environment | Purpose | URL/endpoint | Data |
|---|---|---|---|
| Development | Local development | `<...>` | Synthetic/local |
| Staging | Verification | `<...>` | Non-production |
| Production | Real users | `<...>` | Production |

## 2. Runtime components

`<API / worker / database / cache / storage / broker>`

## 3. Containerization

`<Docker images, compose, registry>`

## 4. Configuration

`<Environment variables / secret manager>`

## 5. Database migrations

`<When/how migrations run>`

## 6. Health checks

- Liveness
- Readiness
- Dependency checks

## 7. CI/CD

`<Pipeline>`

## 8. Rollback

`<Strategy>`

## 9. Backup / disaster recovery

`<RPO/RTO and restoration procedure>`
