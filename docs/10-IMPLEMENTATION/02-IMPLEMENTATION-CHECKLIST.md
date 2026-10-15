# Implementation Checklist — strict gate

## Gate A — Database
- [x] PostgreSQL 17 running; clean migration script applied successfully.
- [x] All PK/FK are NanoID(21).
- [x] No integer/UUID business IDs.
- [x] Required FKs exist (43 verified in PostgreSQL catalog; all 43 FK columns are indexed).
- [x] Unique constraints reviewed.
- [x] Check constraints reviewed (132 table checks verified in PostgreSQL catalog, excluding EF migration metadata).
- [x] Indexes reviewed.
- [x] Money uses numeric(12,2).
- [x] UTC timestamptz.
- [x] Transaction boundaries defined in `02-DATABASE/04-CONSTRAINTS.md`.

The migration chain creates 32 application tables. `FullSchemaPostgreSqlMigrationTests` applies all migrations to a disposable PostgreSQL 17 Testcontainers instance and verifies 32 application tables, 32 `varchar(21)` ID columns, 43 foreign keys whose full column sets are covered by valid non-partial leading index keys, 132 check constraints, and rejection of a malformed NanoID. The schema-contract tests also verify promotion max-discount restrictions, provider transaction ID uniqueness, active-only push-token uniqueness, and rejection of cross-branch menu-category assignment. This validates schema application/integrity only; it does not certify business-layer transaction workflows or migrate the shared local `yumgo` database.

The registration slice adds a data-only migration that seeds the `customer` role without changing the 32-table schema. Its `Down` intentionally preserves the reference row because the idempotent `Up` may have reused pre-existing data. `CustomerRegistrationStoreTests` verifies user/profile/role writes, uniqueness conflicts and rollback on PostgreSQL Testcontainers; `RegistrationEndToEndTests` applies migrations and exercises the actual HTTP endpoint against a separate disposable PostgreSQL instance. Neither test migrates or resets the shared local `yumgo` database.

The F02 login/session runtime now implements login, refresh rotation, logout, and `GET /auth/me`. Access JWTs default to 15 minutes; the opaque refresh session has a non-sliding, absolute 48-hour deadline. Refresh successors inherit the original deadline; replay revokes descendants, and JWT `sid` validation makes logout/rotation/account suspension invalidate prior access tokens immediately. Success/failure, rotation/reuse, and logout audit events omit credentials and token values. API integration tests exercise these flows against disposable PostgreSQL Testcontainers using the existing raw-SQL `refresh_tokens` and `audit_logs` tables; no schema migration or shared database was applied.

## Gate B — Validation
- [ ] Every request field has type/length/range validation.
- [ ] Cross-field validation defined.
- [ ] Cross-table validation defined.
- [ ] Authorization/scope validation defined.
- [ ] State transition validation defined.
- [ ] Concurrency/idempotency reviewed.

## Gate C — Security
- [ ] URLs use NanoID.
- [ ] No sequential IDs.
- [ ] Hidden IDs treated as untrusted.
- [ ] Actor identity server-derived where possible.
- [ ] Resource ownership enforced.
- [ ] No sensitive secret in audit/log payloads.

## Gate D — Feature
- [ ] Business workflow documented.
- [ ] DB rows written documented.
- [ ] Failure cases documented.
- [ ] Side effects documented.
- [ ] Acceptance tests documented.

Only after Gate A passes may feature implementation begin.
