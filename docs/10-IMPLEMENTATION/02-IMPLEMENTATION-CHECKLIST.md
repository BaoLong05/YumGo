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

Baseline migration currently creates 32 application tables. PostgreSQL migration smoke validation ran against an isolated local PostgreSQL 17 container; it does not certify business-layer transaction workflows.

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
