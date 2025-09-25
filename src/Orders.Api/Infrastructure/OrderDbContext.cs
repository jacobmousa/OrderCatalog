using Microsoft.EntityFrameworkCore;
using Orders.Api.Entities;

namespace Orders.Api.Infrastructure;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(cfg =>
        {
            cfg.ToTable("Orders");
            cfg.HasKey(o => o.Id);
            cfg.Property(o => o.Id).ValueGeneratedOnAdd();
            cfg.Property(o => o.Status).HasConversion<string>();
            // Monetary precision (eliminate truncation warnings)
            cfg.Property(o => o.TotalAmount).HasPrecision(18,2);
            cfg.HasMany(o => o.Items)
               .WithOne()
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.ToTable("OrderItems");
            b.HasKey(i => i.Id);
            b.Property(i => i.UnitPrice).HasPrecision(18,2);
        });
    }
}
