using Microsoft.EntityFrameworkCore.Migrations;

namespace Orders.Api.Migrations;

public partial class RenameOrderItemToOrderItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // If legacy singular table exists, rename it to plural
        migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItem','U') IS NOT NULL AND OBJECT_ID('dbo.OrderItems','U') IS NULL
BEGIN
    EXEC sp_rename 'dbo.OrderItem', 'OrderItems';
END
");
        // Rename index if it still has the old name after table rename
        migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItem_OrderId' AND object_id = OBJECT_ID('dbo.OrderItems'))
BEGIN
    EXEC sp_rename N'dbo.OrderItems.IX_OrderItem_OrderId', N'IX_OrderItems_OrderId', N'INDEX';
END
");
        // Drop old-named FK and add new-named FK
        migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItem_Orders_OrderId')
BEGIN
    ALTER TABLE dbo.OrderItems DROP CONSTRAINT FK_OrderItem_Orders_OrderId;
END
IF OBJECT_ID('dbo.OrderItems','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Orders_OrderId')
BEGIN
    ALTER TABLE dbo.OrderItems ADD CONSTRAINT FK_OrderItems_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE;
END
");
        // Ensure index exists
        migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItems','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId' AND object_id = OBJECT_ID('dbo.OrderItems'))
BEGIN
    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
END
");
        // If plural table still doesn't exist (fresh DB), create it
        migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItems','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderItems](
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [OrderId] UNIQUEIDENTIFIER NOT NULL,
        [ProductId] UNIQUEIDENTIFIER NOT NULL,
        [Sku] NVARCHAR(MAX) NOT NULL,
        [Qty] INT NOT NULL,
        [UnitPrice] DECIMAL(18,2) NOT NULL);
    CREATE INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems]([OrderId]);
    ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE;
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Attempt to revert to legacy singular naming (dev-only rollback)
        migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItems','U') IS NOT NULL AND OBJECT_ID('dbo.OrderItem','U') IS NULL
BEGIN
    -- Drop new-named FK and index
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Orders_OrderId')
        ALTER TABLE dbo.OrderItems DROP CONSTRAINT FK_OrderItems_Orders_OrderId;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId' AND object_id = OBJECT_ID('dbo.OrderItems'))
        DROP INDEX IX_OrderItems_OrderId ON dbo.OrderItems;
    EXEC sp_rename 'dbo.OrderItems', 'OrderItem';
END
");
        // Recreate legacy FK and index names if table exists
        migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItem','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItem_OrderId' AND object_id = OBJECT_ID('dbo.OrderItem'))
        CREATE INDEX IX_OrderItem_OrderId ON dbo.OrderItem(OrderId);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItem_Orders_OrderId')
        ALTER TABLE dbo.OrderItem ADD CONSTRAINT FK_OrderItem_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE;
END
");
    }
}
