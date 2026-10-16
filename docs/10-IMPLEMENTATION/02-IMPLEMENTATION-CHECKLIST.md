# Implementation Checklist — strict gate

## Gate A — Database
- [x] PostgreSQL 17 running; clean migration script applied successfully.
- [x] All PK/FK are NanoID(21).
- [x] No integer/UUID business IDs.
- [x] Required FKs exist (43 verified in PostgreSQL catalog; all 43 FK columns are indexed).
- [x] Unique constraints reviewed.
- [x] Check constraints reviewed (134 table checks verified in PostgreSQL catalog, excluding EF migration metadata).
- [x] Indexes reviewed.
- [x] Money uses numeric(12,2).
- [x] UTC timestamptz.
- [x] Transaction boundaries defined in `02-DATABASE/04-CONSTRAINTS.md`.

The migration chain creates 33 application tables, including the Google Geocoding usage meter. `FullSchemaPostgreSqlMigrationTests` applies all migrations to a disposable PostgreSQL 17 Testcontainers instance and verifies 33 application tables, 32 `varchar(21)` ID columns, 43 foreign keys whose full column sets are covered by valid non-partial leading index keys, 134 check constraints, and rejection of a malformed NanoID. The schema-contract tests also verify promotion max-discount restrictions, provider transaction ID uniqueness, active-only push-token uniqueness, and rejection of cross-branch menu-category assignment. This validates schema application/integrity only; it does not certify business-layer transaction workflows or migrate the shared local `yumgo` database.

The registration slice adds a data-only migration that seeds the `customer` role without changing the 32-table schema. Its `Down` intentionally preserves the reference row because the idempotent `Up` may have reused pre-existing data. `CustomerRegistrationStoreTests` verifies user/profile/role writes, uniqueness conflicts and rollback on PostgreSQL Testcontainers; `RegistrationEndToEndTests` applies migrations and exercises the actual HTTP endpoint against a separate disposable PostgreSQL instance. Neither test migrates or resets the shared local `yumgo` database.

The F02 login/session runtime now implements login, refresh rotation, logout, and `GET /auth/me`. Access JWTs default to 15 minutes; the opaque refresh session has a non-sliding, absolute 48-hour deadline. Refresh successors inherit the original deadline; replay revokes descendants, and JWT `sid` validation makes logout/rotation/account suspension invalidate prior access tokens immediately. Success/failure, rotation/reuse, and logout audit events omit credentials and token values. API integration tests exercise these flows against disposable PostgreSQL Testcontainers using the existing raw-SQL `refresh_tokens` and `audit_logs` tables; no schema migration or shared database was applied.

The initial F05 restaurant/branch slice implements owner-scoped restaurant creation/listing and branch creation/listing over the existing schema. New restaurants are `pending`, branches are `draft`, and both creation operations write audit rows atomically. The restaurant workflow migration adds the `rejected` restaurant state and an `admin` role seed without adding tables. The API now supports admin-only pending restaurant approval/rejection (with a required rejection reason), owner-scoped branch publish/hide, and full-set staff role assignment using `roles[]` (`manager`/`staff`, including an empty set to remove assignments). PostgreSQL integration coverage verifies authorization, ownership, validation, idempotency, state transitions, and audit effects. A future permissions UI should render the selected roles as checkboxes.

The F06 menu-management API now supports owner/active-staff branch menu reads, category create/update, item create/update, and stock availability transitions. The existing schema's same-branch category constraint is revalidated in the transactional store; active category requirements, price/name/description bounds, staff scope and audit writes are exercised by PostgreSQL end-to-end tests. Sold-out/available operations update only `stock_status`, preserve item `status`, and are idempotent for repeated target values. Category deletion is not exposed; its documented active-item restriction remains applicable if a deletion endpoint is added. No new migration or table was required. The public customer discovery slice is recorded below; client UI remains follow-up work.

The F07 public discovery API slice now includes restaurant search with text/city/district/open-now/category filters and cursor pagination, restaurant/branch detail and branch listing, and a public branch menu filtered to active, orderable data. Public queries hide pending/suspended/closed/deleted resources. `openNow` uses UTC because branches do not yet store a timezone. Radius/distance/rating/popularity sorting and public-menu caching remain unimplemented; see the F07 spec and HTTP API notes for the exact current contract.

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
