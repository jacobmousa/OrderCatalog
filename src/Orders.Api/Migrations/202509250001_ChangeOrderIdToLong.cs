using Microsoft.EntityFrameworkCore.Migrations;

namespace Orders.Api.Migrations;

public partial class ChangeOrderIdToLong : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Destructive change for demo: drop and recreate tables with bigint keys
        migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItems','U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Orders_OrderId')
        ALTER TABLE dbo.OrderItems DROP CONSTRAINT FK_OrderItems_Orders_OrderId;
    DROP TABLE dbo.OrderItems;
END

IF OBJECT_ID('dbo.Orders','U') IS NOT NULL
    DROP TABLE dbo.Orders;

CREATE TABLE [dbo].[Orders](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [CustomerId] [nvarchar](max) NOT NULL,
    [Status] [nvarchar](max) NOT NULL,
    [TotalAmount] [decimal](18,2) NOT NULL,
    [CreatedUtc] [datetime2] NOT NULL,
    [UpdatedUtc] [datetime2] NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE TABLE [dbo].[OrderItems](
    [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
    [OrderId] [bigint] NOT NULL,
    [ProductId] [uniqueidentifier] NOT NULL,
    [Sku] [nvarchar](max) NOT NULL,
    [Qty] [int] NOT NULL,
    [UnitPrice] [decimal](18,2) NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id] ASC)
);

ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD  CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE;
CREATE INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems]([OrderId]);

");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert to Guid-based keys (also destructive)
        migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.OrderItems','U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Orders_OrderId')
        ALTER TABLE dbo.OrderItems DROP CONSTRAINT FK_OrderItems_Orders_OrderId;
    DROP TABLE dbo.OrderItems;
END

IF OBJECT_ID('dbo.Orders','U') IS NOT NULL
    DROP TABLE dbo.Orders;

CREATE TABLE [dbo].[Orders](
    [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
    [CustomerId] [nvarchar](max) NOT NULL,
    [Status] [nvarchar](max) NOT NULL,
    [TotalAmount] [decimal](18,2) NOT NULL,
    [CreatedUtc] [datetime2] NOT NULL,
    [UpdatedUtc] [datetime2] NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE TABLE [dbo].[OrderItems](
    [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
    [OrderId] [uniqueidentifier] NOT NULL,
    [ProductId] [uniqueidentifier] NOT NULL,
    [Sku] [nvarchar](max) NOT NULL,
    [Qty] [int] NOT NULL,
    [UnitPrice] [decimal](18,2) NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id] ASC)
);

ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD  CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE;
CREATE INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems]([OrderId]);

");
    }
}
