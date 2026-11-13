# Harden commerce workflows and finalize the Lustre .NET portfolio

## Problem and resulting behaviour

Ordinary startup previously migrated the configured database and created an administrator with public credentials. Cart/payment/deletion endpoints could mutate data via GET, and checkout reused floating-point cart prices with read-then-write stock updates. The application now starts without database provisioning, enforces protected POST mutations and creates decimal-priced orders with transactional, conditional inventory updates and submission deduplication.

## Changes

- Migrate to tested .NET 10 and ASP.NET Core/EF Core 10.0.15.
- Replace automatic account/database setup with explicit role/status/admin commands and a fresh SQLite-only Development demo.
- Add global antiforgery validation, owner/admin repository guards, safe JSON rendering and login lockout.
- Validate product prices/quantities, preserve referenced products and reject stale stock edits.
- Enforce named order transitions, restore inventory once on eligible cancellation and confirm simulated payments idempotently.
- Decode, bound and normalize image uploads; compensate failed product writes and guard deletion paths.
- Correct paid-sales reporting, category/name search, local client-validation assets, missing-image states and mobile cart overflow.
- Add integration/HTTP/upload/concurrency tests, real Edge screenshots, English documentation and a CI workflow.

## Validation

See `docs/FINAL_REPORT.md` for final local counts and evidence. Release build and isolated SQLite/HTTP tests were run. Edge exercised real registration/login, cart, checkout, history and administrator restrictions at the requested viewport widths. EF model/snapshot consistency and SQL script generation were verified without applying migrations. Resolved dependencies were checked against the official NuGet advisory feed.

## Database review

`PortfolioSafety` is intentionally not applied. Review `docs/sql/preflight.sql` and the generated upgrade SQL. Unique indexes can fail on existing duplicate carts/lines; decimal conversion can round historical float amounts. Existing status identifiers and user accounts are not reassigned/reset.

## Remaining limits

No disposable SQL Server execution, production email, refunds or real payment gateway. No remote CI result is claimed. The duplicated historical project is excluded from build and its seeder's public credentials removed; removal of that folder and untracking the existing `.csproj.user` remain manual because this workspace cannot modify Git metadata and automatic review rejected folder removal.
