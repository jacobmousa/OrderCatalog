using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Orders.Api.Entities;
using Orders.Api.Infrastructure;

namespace Orders.Api.Migrations;

[DbContext(typeof(global::Orders.Api.Infrastructure.OrderDbContext))]
public class OrderDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.0");
        modelBuilder.Entity<Order>(b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd();
            b.Property<string>("CustomerId").IsRequired();
            b.Property<string>("Status").IsRequired();
            b.Property<decimal>("TotalAmount").HasColumnType("decimal(18,2)");
            b.Property<DateTime>("CreatedUtc");
            b.Property<DateTime>("UpdatedUtc");
            b.HasKey("Id");
            b.ToTable("Orders");
            b.OwnsMany(o => o.Items, items =>
            {
                items.Property<Guid>("Id").ValueGeneratedOnAdd();
                items.Property<long>("OrderId");
                items.Property<Guid>("ProductId");
                items.Property<string>("Sku").IsRequired();
                items.Property<int>("Qty");
                items.Property<decimal>("UnitPrice").HasColumnType("decimal(18,2)");
                items.HasKey("Id");
                items.WithOwner().HasForeignKey("OrderId");
                items.ToTable("OrderItems");
                items.HasIndex("OrderId");
            });
        });
    }
}
