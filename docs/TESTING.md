# Testing

## Automated .NET suite

Run `dotnet test ProjetDotNet.sln --configuration Release`. xUnit tests use in-memory SQLite or a uniquely named temporary SQLite file. HTTP tests replace the SQL Server context and use a test-only header authentication handler registered exclusively inside the test project. The application itself contains no authentication bypass.

Coverage includes cart add/decrement/removal, quantity bounds, missing products, user isolation, current server prices, empty/insufficient-stock checkout, rollback of prior deductions, repeated submissions, payment-method validation, order ownership, admin roles, status jumps, one-time cancellation restocking, idempotent simulated payment, paid-cancellation refusal, product CRUD/deletion history, invalid prices/stocks, stale stock forms, sales reports, concurrent SQLite checkout, HTTP GET rejection and missing/valid CSRF tokens. Upload tests cover spoofed extensions, bounds, traversal, missing files and normalized valid images.

The final results and counts are recorded in [FINAL_REPORT.md](FINAL_REPORT.md). Transient TRX output is under ignored `.artifacts/test-results/`. Earlier failed runs found and led to corrections in LINQ translation, missing-directory deletion and a corrupt PNG test fixture; those runs are not presented as the final result.

## Browser suite

```powershell
npm ci --prefix tests/browser
# Start a fresh --demo instance in Development on http://127.0.0.1:5087.
# Set these to the same temporary passwords used to start that instance.
$env:LUSTRE_ADMIN_PASSWORD = $env:Demo__AdminPassword
$env:LUSTRE_CUSTOMER_PASSWORD = $env:Demo__UserPassword
node tests/browser/smoke.cjs
```

Requires locally installed Microsoft Edge. `LUSTRE_BASE_URL` changes the loopback URL; `LUSTRE_BROWSER_CHANNEL` can select another installed Playwright channel. Use this script **only against the isolated demo** because it registers fictitious users and places orders. It does not delete/reset a database.

The suite uses real Identity forms, searches/filters products, adds to cart, submits checkout, reads order history, checks customer denial of admin pages and visits dashboard/products/stocks/orders as an administrator. It inspects overflow at 320, 390, 768 and 1440 px, records JavaScript exceptions and failed local HTTP responses, and writes genuine PNG captures under `docs/screenshots/`. Machine-readable diagnostics are in `.artifacts/browser-report.json`.

A cart overflow found at 320/390 px was corrected with a scrollable table wrapper. Font/icon CDN outages and aborted requests caused by navigation are recorded separately; browser success does not mean every network request completed.

## Provider limits and unexecuted checks

SQL Server migration execution, lock/deadlock behaviour, concurrent stock administration and checkout under SQL Server were not exercised: no disposable SQL Server instance was supplied. SQLite passing is not a SQL Server concurrency guarantee. The SQL migration was generated and the snapshot checked offline. No production or existing local database was used.

GitHub Actions execution requires publication from a terminal allowed to write `.git`; no remote pass is claimed. CI performs restore/audit, Release build, .NET tests and scoped formatting checks. Browser captures currently run locally with Edge, not in CI.

## Dependencies

`node scripts/audit-dependencies.cjs` checks resolved app/test packages against the official NuGet vulnerability feed and writes a point-in-time report. It accepts stable package versions and fails on prereleases it cannot compare safely. Normal CI also enables native NuGet audit during restore. This environment's Windows TLS client failed to reach NuGet; a temporary loopback transport fetched official packages over Node HTTPS, with caches confined to `.tools`. That transport is not a committed package source or a deployment requirement.
