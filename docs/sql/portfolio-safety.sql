BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    ALTER TABLE [OrderDetail] DROP CONSTRAINT [FK_OrderDetail_Product_ProductId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    DROP INDEX [IX_CartDetail_ShoppingCartId] ON [CartDetail];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ShoppingCart]') AND [c].[name] = N'UserId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ShoppingCart] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [ShoppingCart] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Product]') AND [c].[name] = N'Price');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Product] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Product] ALTER COLUMN [Price] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderDetail]') AND [c].[name] = N'UnitPrice');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [OrderDetail] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [OrderDetail] ALTER COLUMN [UnitPrice] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    ALTER TABLE [Order] ADD [CheckoutToken] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CartDetail]') AND [c].[name] = N'UnitPrice');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [CartDetail] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [CartDetail] ALTER COLUMN [UnitPrice] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShoppingCart_UserId] ON [ShoppingCart] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Order_CheckoutToken] ON [Order] ([CheckoutToken]) WHERE [CheckoutToken] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CartDetail_ShoppingCartId_ProductId] ON [CartDetail] ([ShoppingCartId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    ALTER TABLE [OrderDetail] ADD CONSTRAINT [FK_OrderDetail_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113181007_PortfolioSafety'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113181007_PortfolioSafety', N'10.0.15');
END;

COMMIT;
GO
