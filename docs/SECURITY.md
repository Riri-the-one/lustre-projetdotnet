# Security

## Implemented controls

- No public/default administrator password; ordinary startup creates no account or role and applies no migration.
- Explicit administrator provisioning consumes local environment/secrets, checks Identity results and never resets/elevates an existing user.
- Registration assigns only User and checks role-assignment failure. Identity hashes passwords, requires 12 characters plus complexity and counts failed logins toward lockout.
- Administrator controllers enforce the Admin role. Order/cart repositories also check identity/ownership and guard elevated repository operations.
- Unsafe MVC requests require antiforgery validation globally. Commerce mutations require POST. JavaScript sends `X-CSRF-TOKEN`; deletion/quantity forms include request tokens.
- Checkout ignores cart/client prices and computes decimal unit prices from current products. Unique tokens deduplicate submissions; stock/order/cart updates share a transaction.
- Stock edits reject stale displayed quantities; EF concurrency tokens also protect stock quantity, order status and payment state.
- Paid, non-cancelled revenue excludes cancelled orders. Confirming simulated payment is idempotent. Refunds are not implemented.
- Administrator order data is JSON-serialized for JavaScript and HTML-escaped before dynamic insertion.
- Uploads: JPEG/PNG allowlist, signature checks, actual decoding, maximum 1 MB encoded input/output, maximum 4096 per dimension and 8 million pixels, pixel re-encoding without client metadata, random names and basename-only deletion. Partial uploads are compensated if saving the product fails. Missing files are tolerated; other deletion failures are logged/reported.
- Production uses an exception handler and HSTS. Controller error messages exclude internal exception text. Detailed errors remain available only in Development.

## Database safety

No command in this mission migrated, reset or deleted the original SQL Server database. Tests use SQLite; the demo always generates a new path. `docs/sql/preflight.sql` is read-only. The generated upgrade SQL is for review, not automatic execution. Resolve duplicate carts/lines and review float-to-decimal rounding before deliberately applying it on a backed-up, verified connection.

Existing accounts with historically public credentials are **not changed automatically**. The owner must review and rotate/disable any account created by the previous seeder. Removing a source-code password does not revoke an already-created account.

## Operational limits

No real payments, payment-card collection, production email service, refunds or malware scanning. Do not expose the Development demo publicly. Demo data-protection keys and databases are local ignored artifacts; production requires a persistent, protected key store and deployment-specific HTTPS/cookie configuration. CDN fonts/icons and original product images need availability/licensing review.

The dependency check records known NuGet advisories at a point in time; it is not a penetration test or proof that every dependency is safe. The remote CI workflow was prepared but not executed during this session. Re-run dependency auditing before publishing.

## Reporting a concern

Report security concerns privately to the repository owner. Do not include real credentials or personal customer data in a public issue.
