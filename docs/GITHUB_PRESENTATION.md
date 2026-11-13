# GitHub Presentation

## Suggested repository description

Skincare e-commerce portfolio built with C#, ASP.NET Core MVC, EF Core and Identity: role-based administration, transactional checkout, inventory control and integration tests.

## Suggested topics

`csharp`, `dotnet`, `aspnet-core`, `aspnet-mvc`, `entity-framework-core`, `sql-server`, `sqlite`, `identity`, `ecommerce`, `xunit`, `portfolio`

## Interview walkthrough

1. Show the skincare storefront and responsive catalogue.
2. Register/sign in, explain Identity and the distinction between customer and administrator roles.
3. Add products to a private cart; explain indicative prices and non-reserved inventory.
4. Place a simulated order; walk through definitive prices, transaction boundaries, conditional stock UPDATE and the unique checkout token.
5. Show owner-scoped history, administration and valid status transitions.
6. Demonstrate a rejected invalid quantity, unauthorized admin request and CSRF test.
7. Explain why SQLite tests cannot prove SQL Server locking behaviour, and why migrations are explicit.
8. Open the test suite and CI definition; distinguish local evidence from an actual remote workflow run.

Use the screenshots in the README and pin the repository once published. Present the project as a learning/engineering portfolio with simulated payments, not a production shop. Product photography is inherited; review its redistribution rights.

## Honest scope

Do not claim live payment processing, net-profit calculation, production deployment, a security certification or verified SQL Server concurrency. Describe the constraints and proposed disposable SQL Server CI as engineering decisions and remaining work.
