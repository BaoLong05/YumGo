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

## EF Core rule
Generate migrations only from reviewed model + configuration. Do not create a migration for placeholder entities.

The initial commerce schema is currently created by the reviewed SQL in `FullCommerceSchema`, while the EF model snapshot describes only mapped entities (currently `User`). Do not blindly apply a newly scaffolded EF migration for a commerce table already created by that baseline: EF may emit duplicate `CREATE TABLE` operations. When adding EF mappings for baseline tables, reconcile the generated migration and model snapshot with the existing schema, and verify the resulting migration against a clean PostgreSQL database before applying it anywhere persistent. Keep the schema SQL, table specification, migration, and PostgreSQL integration tests in sync.

## Transaction rule
Use one database transaction when one business operation touches multiple aggregate-related rows and partial commit would break business invariants.
