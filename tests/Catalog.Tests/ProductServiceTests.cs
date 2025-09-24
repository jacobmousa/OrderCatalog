using Catalog.Api.Data;
using Catalog.Api.Entities;
using Catalog.Api.Services;
using Catalog.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Xunit;

namespace Catalog.Tests;

public class ProductServiceTests
{
    private static ProductDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductDbContext(options);
    }

    private static IProductService CreateService(ProductDbContext ctx)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ProductService>();
        return new ProductService(ctx, logger);
    }

    [Fact]
    public async Task CreateAsync_ReturnsErrors_WhenInvalid()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var (product, errors) = await svc.CreateAsync(new CreateProductRequest("", "", -1, -1));
        product.Should().BeNull();
        errors.Should().NotBeNull();
        errors!.Keys.Should().Contain(new[]{"Sku","Name","Price","Stock"}, because: "validation should fail for empty and negative values");
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenValid()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var (product, errors) = await svc.CreateAsync(new CreateProductRequest("SKU-1","Test Product", 10m, 5));
        errors.Should().BeNull();
        product.Should().NotBeNull();
        (await ctx.Products.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenDuplicateSku()
    {
        using var ctx = CreateContext();
        ctx.Products.Add(new Product{Sku="SKU-1", Name="Existing", Price=1, Stock=1});
        ctx.SaveChanges();
        var svc = CreateService(ctx);
        var (_, errors) = await svc.CreateAsync(new CreateProductRequest("SKU-1","Another", 2m, 2));
        errors.Should().NotBeNull();
        errors!.Should().ContainKey("Sku");
    }

    [Fact]
    public async Task PatchAsync_UpdatesFields()
    {
        using var ctx = CreateContext();
        var p = new Product{Sku="S1", Name="Name", Price=5, Stock=2};
        ctx.Products.Add(p); ctx.SaveChanges();
        var svc = CreateService(ctx);
        var (updated, errors) = await svc.PatchAsync(p.Id, new UpdateProductRequest(9m, 7, null, null));
        errors.Should().BeNull();
        updated!.Price.Should().Be(9m);
        updated.Stock.Should().Be(7);
    }

    [Fact]
    public async Task ListAsync_FiltersByPriceAndStock()
    {
        using var ctx = CreateContext();
        ctx.Products.AddRange(new []{
            new Product{Sku="A", Name="A", Price=5, Stock=0},
            new Product{Sku="B", Name="B", Price=10, Stock=5},
            new Product{Sku="C", Name="C", Price=15, Stock=2},
            new Product{Sku="D", Name="D", Price=20, Stock=1}
        });
        ctx.SaveChanges();
        var svc = CreateService(ctx);
        var page = await svc.ListAsync(null, 10m, 18m, true, 1, 50);
        page.Items.Should().AllSatisfy(p => { p.Price.Should().BeGreaterOrEqualTo(10m); p.Price.Should().BeLessOrEqualTo(18m); p.Stock.Should().BeGreaterThan(0); });
    page.Items.Should().OnlyContain(p => p.Sku == "B" || p.Sku == "C");
    }

    [Fact]
    public async Task DeleteAsync_RemovesProduct()
    {
        using var ctx = CreateContext();
        var p = new Product{Sku="S1", Name="Name", Price=5, Stock=2};
        ctx.Products.Add(p); ctx.SaveChanges();
        var svc = CreateService(ctx);
        var removed = await svc.DeleteAsync(p.Id);
        removed.Should().BeTrue();
        (await ctx.Products.CountAsync()).Should().Be(0);
    }
}
