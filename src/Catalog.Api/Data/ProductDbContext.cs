using Catalog.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(cfg =>
        {
            cfg.HasKey(p => p.Id);
            cfg.Property(p => p.Sku).IsRequired().HasMaxLength(64);
            cfg.Property(p => p.Name).IsRequired().HasMaxLength(256);
            cfg.Property(p => p.Price).HasColumnType("decimal(18,2)");
            cfg.HasIndex(p => p.Sku).IsUnique();
        });
    }
}
