-- READ-ONLY preflight for the PortfolioSafety migration.
-- Select and back up the intended database yourself. Do not run against an unverified connection.
SELECT UserId, COUNT(*) AS CartCount FROM ShoppingCart GROUP BY UserId HAVING COUNT(*) > 1;
SELECT ShoppingCartId, ProductId, COUNT(*) AS LineCount
FROM CartDetail GROUP BY ShoppingCartId, ProductId HAVING COUNT(*) > 1;
SELECT Id, Price FROM Product WHERE Price <= 0 OR Price > 999999.99 OR Price <> ROUND(Price, 2);
SELECT Id, UnitPrice FROM OrderDetail WHERE UnitPrice < 0 OR UnitPrice <> ROUND(UnitPrice, 2);
SELECT Id, Quantity FROM Stock WHERE Quantity < 0;
SELECT Id, StatusId, StatusName FROM OrderStatus ORDER BY Id;
-- Resolve duplicate carts/lines deliberately before creating unique indexes.
-- Decimal conversion can round historical float values: review amounts before applying SQL.
