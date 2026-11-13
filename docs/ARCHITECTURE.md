# Architecture

Lustre retains one ASP.NET Core MVC project and one integration-test project. Razor Pages implement Identity; MVC controllers serve the storefront and administration. Repositories use the scoped `ApplicationDbContext` directly. This is intentionally a small application, not a distributed system.

## Main responsibilities

| Component | Responsibility |
| --- | --- |
| HomeController / HomeRepository | Read-only catalogue, name search and category filters |
| CartController / CartRepository | Authenticated cart access, quantity validation and atomic checkout |
| UserOrderController / UserOrderRepository | Owner-scoped history; administrator order/payment transitions |
| Product / Category / Stock controllers and repositories | Administrator-only catalogue and inventory management |
| ReportRepository | Paid, non-cancelled unit-sales ranking using provider-independent LINQ |
| FileService | Bounded image validation, decode/re-encode, generated filenames and safe deletion |
| DbSeeder / AdminProvisioner | Explicit reference-data/account provisioning, never normal startup |
| DemoData | Fictitious accounts and products in a new Development-only SQLite database |

## Relationships and constraints

A Category has many Products. Each Product has at most one Stock record. A ShoppingCart belongs to an Identity user ID and has many CartDetails, unique by cart/product. A user has at most one cart. An Order belongs to a user ID, references an existing OrderStatus and has OrderDetails. Each order line records quantity and a definitive decimal unit price. Product deletion through order lines is restricted.

Money uses `decimal` with SQL Server precision `(18,2)`. `CheckoutToken` is nullable for historical orders and unique for new submissions. Existing status primary keys and business codes are retained; transitions resolve names, not assumed IDs.

## Checkout transaction

1. Validate contact fields, payment method and a nonempty submission token.
2. Begin a serializable transaction; recognize an already completed submission for this owner.
3. Read the owner's cart and current product prices. Reject empty/invalid carts.
4. For each product in a consistent ID order, execute `UPDATE Stock SET Quantity = Quantity - requested WHERE Quantity >= requested`.
5. Create the order and its lines, consume cart lines, save and commit.
6. On a stock/database failure, roll back and clear tracked state. Return a generic failure; do not automatically replay a deadlocked transaction.

This combines conditional writes, transaction isolation, unique constraints and submission tokens. A SQLite concurrent-checkout test is included. SQL Server correctness still requires execution against a disposable SQL Server instance; no provider-equivalence claim is made.

## Startup modes

Normal startup configures middleware and routes only. `--provision-roles` explicitly provisions missing roles/statuses against the selected schema. `--provision-admin` creates a new account from local secrets and refuses existing accounts. Neither applies migrations. `--demo` uses `EnsureCreated` only on a freshly generated SQLite path under `.artifacts`; it cannot run outside Development.

## Tradeoffs

Controllers and repositories share commerce responsibilities; extracting a dedicated checkout service can follow if scope grows. Product names are read from current products in order history, while prices are snapshotted. Cart prices do not reserve stock. Cancellation is supported only before shipping and only for unpaid orders; refunds are intentionally absent.
