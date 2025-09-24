using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Catalog.Api.Data;
using Catalog.Api.Entities;

namespace Catalog.Api.Migrations;

[DbContext(typeof(ProductDbContext))]
public class ProductDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.0");
        modelBuilder.Entity<Product>(b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd();
            b.Property<string>("Sku").IsRequired().HasMaxLength(64);
            b.Property<string>("Name").IsRequired().HasMaxLength(256);
            b.Property<decimal>("Price").HasColumnType("decimal(18,2)");
            b.Property<int>("Stock");
            b.Property<DateTime>("CreatedUtc");
            b.Property<DateTime>("UpdatedUtc");
            b.HasKey("Id");
            b.HasIndex("Sku").IsUnique();
            b.ToTable("Products");
        });
    }
}
