# Final Report — Lustre Portfolio Finalization

Verified locally on **13 November 2026**, branch `feat/lustre-portfolio-finalization`.

## 1. Implementation outcome

The root application was migrated, hardened and exercised locally. The existing MVC/repository design and skincare identity were retained. The final local Release build has **0 errors and 0 warnings**, and **61 automated tests pass**. Git publication remains manual under the session's read-only `.git` permissions.

Machine-readable evidence: [validation summary](validation/summary.json), [browser report](validation/browser-report.json), [NuGet advisory report](validation/dependency-audit.json).

## 2. Audit findings and priorities

The initial application targeted net9.0 and used mixed Microsoft 9.0 package versions, floating-point money, an automatically seeded public administrator and migrations during startup. Cart, deletion and simulated payment actions lacked explicit unsafe verbs. An administrator AJAX action disabled antiforgery. Checkout used cached cart prices and a read-then-write stock decrement; several catches masked failures. Reports depended on an undocumented stored procedure. Client validation scripts were referenced but absent. A historical nested copy and a tracked `.csproj.user` complicated repository hygiene.

Security, checkout integrity and tests were prioritized before visual refinements. No microservices or additional application layers were introduced.

## 3. .NET version and dependencies

The system initially had **no SDK**, only .NET 8/9 runtimes. Official SDK **10.0.404** was downloaded into ignored `.tools/dotnet`, without a system-wide install. The root application and tests target **net10.0**, with ASP.NET Core Identity and EF Core **10.0.15**. Unused scaffolding dependencies and their accidental Humanizer/Microsoft.Build imports were removed.

Windows' TLS client could not restore NuGet directly. A temporary localhost transport retrieved official NuGet packages using Node HTTPS; no TLS verification was disabled and no temporary feed configuration was committed. All caches are under ignored `.tools`.

The final advisory check inspected **110 resolved app/test packages**, with **0 known advisory matches** at the recorded time. Both browser-tool and bundled-JavaScript npm audits reported **0 vulnerabilities**. ImageSharp was evaluated but is not in the final graph; the final codec uses StbImageSharp/StbImageWriteSharp. Re-run audits before publication; point-in-time results are not a security guarantee.

## 4. Security corrections

- Removed startup migration/seeding and hardcoded administrator credentials, including the duplicated snapshot's seeder.
- Added explicit role/status provisioning and local-secret administrator creation that refuses existing accounts.
- Increased passwords to 12 characters, enabled login-failure lockout and checked registration role assignment.
- Required POST for commerce mutations and global antiforgery validation, including AJAX headers.
- Added owner/admin checks inside order/cart repositories, safe JSON serialization and HTML encoding for dynamic order details.
- Replaced browser-facing exception details with generic messages and logging.
- Added bounded image signature checks, full decode, dimension/pixel limits, metadata-free re-encoding, generated names, path guards and failure cleanup.

Existing users and previously created administrators were not modified. The owner must separately review and rotate/disable historically public credentials on any existing account.

## 5. Cart and checkout

Cart operations enforce the current user, product existence, positive bounded quantities and stock availability. Unique cart/user and cart/product constraints prevent duplicate rows after the deliberate schema upgrade. Browser add actions suppress concurrent double clicks.

Cart prices remain indicative and do not reserve inventory. Checkout reads definitive prices from current products, uses decimal order-line snapshots and executes conditional stock deductions in a serializable transaction. Empty/invalid/insufficient carts are rejected. Failures roll back earlier deductions and order creation. A unique submission token recognizes repeated successful submissions without consuming a subsequently refilled cart.

## 6. Orders, inventory and reporting

Status transitions follow existing status names and preserve their IDs. Pending/Approved unpaid orders may be cancelled; stock is restored once transactionally. Shipped orders may become Delivered; terminal orders cannot be reopened. Paid cancellation is refused pending a refund implementation.

Simulated payment confirmation is idempotent. Stock editing rejects negative values, missing products and stale displayed quantities; EF concurrency tokens protect intervening writes. Referenced products/categories cannot be deleted. Revenue excludes cancelled/unpaid orders and is labelled revenue, not profit. Missing stock records count as low stock. Sales reports use LINQ and paid, non-cancelled orders.

## 7. Database and migration validation

Generated `PortfolioSafety` and an idempotent SQL upgrade script. EF reports **no pending model changes**. No migration was applied to any SQL Server database. No original Ecom data was read, reset or altered by the test/demo runs.

Review `docs/sql/preflight.sql` and `docs/sql/portfolio-safety.sql`. Duplicate carts/lines require deliberate resolution; float-to-decimal conversion can round old values. The demo uses `EnsureCreated` only on a newly generated SQLite path; it does not execute SQL Server migrations.

## 8. UI/UX

Preserved beige, off-white and brown styling. Corrected mobile cart overflow, responsive minimum widths, focus visibility, reduced-motion behaviour, product placeholders, image fitting, accessible control labels and keyboard access to order selection. Removed decorative carousel controls, false newsletter behaviour and dead social links. Bundled missing client-validation libraries and Bootstrap JavaScript locally, retaining licence notices. The dashboard uses MAD consistently.

## 9. Automated test results

**61 passed, 0 failed, 0 skipped** in the final suite (approximately 38 seconds).

Tests cover customer/admin/anonymous HTTP access, owner isolation, GET rejection, antiforgery, cart quantities, missing products, current server prices, empty checkout, insufficient stock, transaction rollback, repeated submissions, payment validation, product CRUD/history protection, stale/invalid inventory, named lifecycle transitions, one-time restocking, simulated payment, sales reports, reference-data ID preservation and uploads. A concurrent **SQLite** test verifies that two checkouts cannot both purchase the last item.

Initial failures in report-query translation, missing-directory image deletion and a corrupt PNG fixture were corrected. The final suite has no remaining failure. The test runner emits a non-failing artifact-directory compatibility notice; TRX output was produced successfully.

## 10. Browser validation and captures

**17 browser checks passed** in Microsoft Edge through Playwright using real Identity registration/login and a fresh fictitious demo. This includes protected cart JavaScript, checkout, customer history, customer denial of admin access, administrator product/stock views, AJAX status updates and simulated payment confirmation.

Inspected **320, 390, 768 and 1440 px**. Final diagnostics show **0 JavaScript exceptions, 0 failed local HTTP responses and 0 horizontal page overflow** on the checked pages. An aborted cart-count request during navigation is recorded separately, not hidden.

**10 real PNG captures**: home desktop, catalogue, cart, checkout, orders, admin dashboard, admin products, admin stock, admin orders and mobile. They are in `docs/screenshots/`; hashes are included in the validation summary. No synthetic screenshot was created.

## 11. Unexecuted checks and limits

Docker CLI exists but its daemon pipe is unavailable; access to its external config is also denied. No disposable SQL Server instance was available. SQL Server schema execution, locking/deadlocks and concurrent provider behaviour therefore remain **unverified**. SQLite results are not presented as SQL Server guarantees.

No live payments, refunds, production email, deployment or full accessibility/penetration audit. Keyboard/focus improvements and responsive checks are useful coverage, not certification. Inherited product imagery requires redistribution-rights review. External fonts/icons still depend on CDNs.

## 12. Repository hygiene and CI

Expanded `.gitignore` for user settings, generated output, local databases, secrets/config overrides, logs, node_modules and tooling artifacts. The historical `dotnetProj-main/` copy is excluded from compilation/content. It has no uniquely named files relative to the root project; deletion was rejected by automatic approval review and not retried. The tracked `.csproj.user` also remains for manual untracking.

Scoped C# whitespace verification, known-credential pattern scanning and **`git diff --check` (exit 0)** pass. Git's normal Windows line-ending notices are non-failing. Local tooling/databases/password files are ignored and not proposed for Git publication.

CI is prepared for restore with NuGet audit, Release build, tests, scoped formatting, dependency/credential-pattern checks and result artifacts. **No remote workflow execution is claimed.**

## 13. Documentation delivered

English README; ARCHITECTURE, SECURITY, TESTING, FINAL_REPORT, GITHUB_PRESENTATION, PR_DESCRIPTION, PUBLISHING and THIRD_PARTY documents; reviewed migration/preflight SQL; ten real screenshots; versioned local-validation summaries; EF tool manifest and .NET SDK policy.

## 14. Git publication state

Working branch: `feat/lustre-portfolio-finalization`. All changes remain **uncommitted locally**. No commit hash, remote push, PR URL or GitHub Actions result exists for this intervention. `.git` is read-only by the supplied permission profile; no attempt was made to force permissions, rewrite history, merge or deploy.

## 15. Essential manual actions

1. Open a normal terminal, review/untrack `ProjetDotNet.csproj.user`, and review/remove the historical duplicate if appropriate.
2. Review any previously seeded administrator account and rotate/disable its old credentials without resetting legitimate users.
3. Use `docs/PUBLISHING.md` to revalidate, commit, push and open the PR; then inspect actual remote CI results.
4. Before using SQL Server with the changed model, back up and explicitly verify the intended connection, resolve preflight findings and test the generated upgrade on a disposable copy first. No automatic migration is required to try the isolated demo.
