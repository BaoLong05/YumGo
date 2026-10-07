# YumGo

YumGo is a food ordering and delivery product, organized as a monorepo for its ASP.NET Core API, web client, mobile app, and shared TypeScript contracts.

## Architecture and stack

- Backend: .NET 10, ASP.NET Core, Clean Architecture, PostgreSQL, Redis.
- Web: Next.js, TypeScript, Tailwind CSS, Bun.
- Mobile: React Native, Expo, Expo Router, TypeScript, Effect, Bun.
- Shared client contracts: `@yumgo/contracts`.
- Local infrastructure: Docker Compose.

The backend projects are split into Domain, Application, Infrastructure, and API. This repository is at the bootstrap stage; product workflows and authentication are not implemented.

## Repository structure

- `backend/` — .NET solution, source projects, and tests.
- `web/yumgo-web/` — Next.js web application.
- `mobile/yumgo-mobile/` — the single Expo mobile application.
- `packages/contracts/` — shared client-facing TypeScript contracts.
- `infra/` — local infrastructure support files.
- `docs/` — project and architecture documentation.
- `scripts/` — developer scripts.

## Prerequisites

- .NET 10 SDK
- Bun 1.3.14
- Docker Desktop with the Linux container engine running
- Expo-compatible device/emulator for native mobile development

Copy each app's `.env.example` to its local environment file as needed. Do not commit local environment files or production credentials.

## Local development

Start PostgreSQL and Redis from the repository root:

```sh
docker compose up -d
docker compose ps
```

Run the backend API:

```sh
cd backend
dotnet restore
dotnet run --project src/YumGo.Api
```

The API health endpoint is `/health`; OpenAPI is available at `/openapi/v1.json` in Development.

Run the web app:

```sh
cd web/yumgo-web
bun install
bun run dev
```

Run the mobile app:

```sh
cd mobile/yumgo-mobile
bun install
bun start
```

For Expo Go compatibility, native modules that are not bundled in Expo Go require a development build.

## Tests and checks

```sh
cd backend && dotnet build && dotnet test
cd web/yumgo-web && bun run build && bun run lint
cd mobile/yumgo-mobile && bunx tsc --noEmit && bunx expo-doctor
cd packages/contracts && bun install
```