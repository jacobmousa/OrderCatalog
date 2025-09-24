using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Orders.Api.Services;
using Orders.Api.Infrastructure;
using Orders.Api.Entities;
using Orders.Api.Dtos;
using Orders.Api.Exceptions;
using Xunit;

namespace Orders.Tests;

public class OrderServiceTests
{
    private static OrderDbContext CreateContext()
    {
        // Use SQLite in-memory to better mimic relational behavior and avoid InMemory concurrency quirks
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new OrderDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static IOrderService CreateService(OrderDbContext ctx, HttpMessageHandler? handler = null)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<OrderService>();
        var factory = CreateHttpClientFactory(handler ?? new StubHandler());
        return new OrderService(ctx, factory, logger);
    }

    private static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler){ BaseAddress = new Uri("http://localhost") };
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return mock.Object;
    }

    private class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Minimal fake product response
            if (request.RequestUri!.AbsolutePath.StartsWith("/api/products"))
            {
                var json = "{\"id\":\"00000000-0000-0000-0000-000000000123\",\"sku\":\"SKU-1\",\"name\":\"Demo\",\"price\":10.5,\"stock\":5}";
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){ Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
            }
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
    }

    [Fact]
    public async Task CreateDraftAsync_SetsCustomerId_WhenMissing()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var order = await svc.CreateDraftAsync(null);
        order.CustomerId.Should().NotBeNullOrWhiteSpace();
        ctx.Orders.Count().Should().Be(1);
    }

    [Fact]
    public async Task AddItemAsync_AddsNewLine_WhenFirstAddition()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var order = await svc.CreateDraftAsync("cust1");
        var updated = await svc.AddItemAsync(order.Id, new AddOrderItemRequest(null, "SKU-1", 2));
        updated.Items.Should().HaveCount(1);
        updated.TotalAmount.Should().Be(21m); // 10.5 * 2
    }

    [Fact]
    public async Task AddItemAsync_IncrementsQuantity_WhenSameProduct()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var order = await svc.CreateDraftAsync("cust1");
        await svc.AddItemAsync(order.Id, new AddOrderItemRequest(null, "SKU-1", 2));
        var updated = await svc.AddItemAsync(order.Id, new AddOrderItemRequest(null, "SKU-1", 3));
        updated.Items.Single().Qty.Should().Be(5);
    }

    [Fact]
    public async Task AddItemAsync_Throws_WhenQtyInvalid()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var order = await svc.CreateDraftAsync("cust1");
        Func<Task> act = () => svc.AddItemAsync(order.Id, new AddOrderItemRequest(null, "SKU-1", 0));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ConfirmAsync_ChangesStatus()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var order = await svc.CreateDraftAsync("cust1");
        await svc.AddItemAsync(order.Id, new AddOrderItemRequest(null, "SKU-1", 1));
        var confirmed = await svc.ConfirmAsync(order.Id);
        confirmed.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task CancelAsync_SetsCancelled()
    {
        using var ctx = CreateContext();
        var svc = CreateService(ctx);
        var order = await svc.CreateDraftAsync("cust1");
        var cancelled = await svc.CancelAsync(order.Id);
        cancelled.Status.Should().Be(OrderStatus.Cancelled);
    }
}
