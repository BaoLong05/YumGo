# PostgreSQL Development Setup

```yaml
services:
  postgres:
    image: postgres:17-alpine
    container_name: yumgo-postgres
    environment:
      POSTGRES_DB: yumgo
      POSTGRES_USER: yumgo
      POSTGRES_PASSWORD: yumgo_dev_password
    ports:
      - "5432:5432"
    volumes:
      - yumgo_postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U yumgo -d yumgo"]
      interval: 5s
      timeout: 5s
      retries: 10

volumes:
  yumgo_postgres_data:
```

## Connection
```text
Host=localhost;Port=5432;Database=yumgo;Username=yumgo;Password=yumgo_dev_password
```

## Schema and migration ownership
PostgreSQL is the database authority. The checked-in schema SQL is the canonical target specification; runtime schema changes are applied through the versioned migration chain. The commerce baseline is SQL-managed because the current EF model snapshot tracks only mapped entities (currently `User`). Do not scaffold a model migration for a commerce table that already exists in the SQL baseline.

For SQL-managed schema changes, author a reviewed migration and update the canonical SQL/specification plus PostgreSQL integration tests in the same change. For a future migration that maps an existing commerce table into EF, first reconcile the model snapshot with the live migration target so EF does not scaffold duplicate `CREATE TABLE` operations. `dotnet ef has-pending-model-changes` only checks mapped EF model state; it does not detect drift in raw-SQL-managed tables. Verify all changes against a disposable clean PostgreSQL database before applying them anywhere persistent.

## Transaction rule
Use one database transaction when one business operation touches multiple aggregate-related rows and partial commit would break business invariants.
