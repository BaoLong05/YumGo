# YumGo documentation

YumGo is a monorepo for an ASP.NET Core backend, a Next.js web client, one Expo mobile application, and shared client-facing TypeScript contracts.

## Current architecture

The backend is split into Domain, Application, Infrastructure, and API projects. PostgreSQL is the relational database and Redis is the local cache service. The web application uses Next.js App Router and TypeScript. The single mobile app uses Expo Router, React Native, TypeScript, and Effect. Shared contracts live in `packages/contracts`.

This repository is at bootstrap stage. Authentication and product workflows are not implemented.

## Technology stack

- .NET 10 / ASP.NET Core
- Entity Framework Core / PostgreSQL / Redis
- Next.js / TypeScript / Tailwind CSS / Bun
- React Native / Expo / Expo Router / Effect / Bun
- Docker Compose

## Repository structure

See the root README for the top-level directories. The backend solution is in `backend/YumGo.slnx`; clients are under `web/yumgo-web` and `mobile/yumgo-mobile`.

## Local prerequisites

Install .NET 10 SDK, Bun 1.3.14, and Docker Desktop. Start Docker Desktop's Linux container engine before starting local services. For mobile native modules, use an Expo development build or a compatible native runtime.

## Run Docker services

From the repository root:

```sh
docker compose up -d
docker compose ps
```

PostgreSQL is exposed on port 5432 and Redis on port 6379. Credentials in Compose are for local development only.

## Run backend

```sh
cd backend
dotnet restore
dotnet run --project src/YumGo.Api
```

The API health endpoint is `/health`; development OpenAPI is at `/openapi/v1.json`.

## Run web

```sh
cd web/yumgo-web
bun install
bun run dev
```

## Run mobile

```sh
cd mobile/yumgo-mobile
bun install
bun start
```

## Tests and checks

```sh
cd backend && dotnet build && dotnet test
cd web/yumgo-web && bun run build && bun run lint
cd mobile/yumgo-mobile && bunx tsc --noEmit && bunx expo-doctor
cd packages/contracts && bun install
```

Copy `.env.example` to the appropriate local environment file when needed. Never commit `.env` files, tokens, or production credentials.
