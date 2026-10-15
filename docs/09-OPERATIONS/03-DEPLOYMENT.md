# 17. Deployment and Environment Specification

## 17.1 Environments

- `local`
- `staging`
- `production`

## 17.2 Local infrastructure

Docker Compose provides:
- PostgreSQL
- Redis
- message broker when async infrastructure is enabled

## 17.3 Production components

Minimum production components:
- ASP.NET Core API instances
- PostgreSQL
- Redis
- durable broker
- Next.js web deployment
- mobile distribution pipeline
- object storage if media uploads are enabled
- observability backend

## 17.4 Configuration

Use strongly typed options for:
- Database
- Redis
- JWT
- Payment provider
- Map provider
- Notifications
- Broker
- CORS

## 17.5 Migrations

Database migrations are applied through a controlled deployment step. Production schema changes must be backward compatible when application instances may overlap during rolling deployment.

## 17.6 Startup behavior

API startup must:
1. load configuration
2. validate required options
3. build dependency graph
4. register health checks
5. start serving requests

Automatic destructive database creation is forbidden in production.

## 17.7 CI pipeline

Required stages:

```text
Restore
 ↓
Build
 ↓
Static/type checks
 ↓
Unit tests
 ↓
Integration tests
 ↓
Security/dependency checks
 ↓
Build container
 ↓
Publish artifact
```

## 17.8 Containerization

Use a multi-stage Docker build for the API.

Runtime image should contain only what is required to run the application.

## 17.9 Rollback

Deployment must keep the previous application artifact available.

Database migrations must be designed so application rollback is possible without data corruption.

## 17.10 Backups

Production PostgreSQL requires:
- scheduled backups
- retention policy
- tested restore procedure

A backup that has never been restored in a test is not considered verified.
