import { randomBytes } from "node:crypto";
import { resolve } from "node:path";

const root = resolve(import.meta.dir, "..");
const env = {
  ...process.env,
  ASPNETCORE_ENVIRONMENT: process.env.ASPNETCORE_ENVIRONMENT ?? "Development",
  "ConnectionStrings__DefaultConnection":
    process.env.ConnectionStrings__DefaultConnection ??
    "Host=localhost;Port=5434;Database=yumgo;Username=yumgo;Password=yumgo_dev_password",
  "Authentication__Jwt__Issuer":
    process.env.Authentication__Jwt__Issuer ?? "YumGo.Local",
  "Authentication__Jwt__Audience":
    process.env.Authentication__Jwt__Audience ?? "YumGo.Local.Client",
  "Authentication__Jwt__SigningKey":
    process.env.Authentication__Jwt__SigningKey || randomBytes(32).toString("base64"),
};

console.log("Starting local PostgreSQL and Redis...");
const dependencies = Bun.spawnSync(
  ["docker", "compose", "up", "-d", "--wait", "postgres", "redis"],
  { cwd: root, stdout: "inherit", stderr: "inherit" },
);
if (dependencies.exitCode !== 0) process.exit(dependencies.exitCode);

console.log("Applying local database migrations...");
const migrations = Bun.spawnSync(
  [
    "dotnet",
    "ef",
    "database",
    "update",
    "--project",
    "backend/src/YumGo.Infrastructure",
    "--startup-project",
    "backend/src/YumGo.Api",
  ],
  { cwd: root, env, stdout: "inherit", stderr: "inherit" },
);
if (migrations.exitCode !== 0) process.exit(migrations.exitCode);

console.log("Starting YumGo API at http://localhost:5000");
const server = Bun.spawn(
  ["dotnet", "run", "--project", "backend/src/YumGo.Api", "--launch-profile", "http"],
  { cwd: root, env, stdin: "inherit", stdout: "inherit", stderr: "inherit" },
);

process.on("SIGINT", () => server.kill("SIGINT"));
process.on("SIGTERM", () => server.kill("SIGTERM"));
process.exit(await server.exited);
