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

## Transaction rule
Use one database transaction when one business operation touches multiple aggregate-related rows and partial commit would break business invariants.
