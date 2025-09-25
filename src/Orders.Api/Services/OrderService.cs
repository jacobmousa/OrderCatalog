using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Dtos;
using Orders.Api.Entities;
using Orders.Api.Exceptions;
using Orders.Api.Infrastructure;

namespace Orders.Api.Services;

public interface IOrderService
{
    Task<Order> CreateDraftAsync(string? customerId);
    Task<Order> AddItemAsync(long orderId, AddOrderItemRequest request);
    Task<Order> ConfirmAsync(long id);
    Task<Order> CancelAsync(long id);
}

public class OrderService : IOrderService
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrderService> _logger;
    private const string CorrelationHeader = "x-correlation-id";
    public OrderService(OrderDbContext db, IHttpClientFactory httpClientFactory, ILogger<OrderService> logger)
    { _db = db; _httpClientFactory = httpClientFactory; _logger = logger; }

    public async Task<Order> CreateDraftAsync(string? customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId)) customerId = Guid.NewGuid().ToString();
        var order = new Order { CustomerId = customerId };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> AddItemAsync(long orderId, AddOrderItemRequest request)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId) ?? throw new OrderNotFoundException();
        order.EnsureDraft();
        if (request.Qty <= 0) throw new ValidationException("qty", "Qty must be > 0");
        // Resolve product by id or sku
        ProductDto? product = null;
        if (request.ProductId is not null)
        {
            product = await GetProductAsync($"/api/products/{request.ProductId}");
        }
        else if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            product = await GetProductAsync($"/api/products/sku/{request.Sku}");
        }
        else
        {
            throw new ValidationException("product", "Either productId or sku is required.");
        }
        if (product is null) throw new ValidationException("product", "Product not found in catalog.");
        var existing = order.Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is null)
        {
            var newItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Sku = product.Sku,
                Qty = request.Qty,
                UnitPrice = product.Price
            };
            _db.OrderItems.Add(newItem); // relationship fixup will populate order.Items
        }
        else
        {
            existing.Qty += request.Qty; // simple merge
        }
        order.RecalculateTotal();
        order.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> ConfirmAsync(long id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id) ?? throw new OrderNotFoundException();
        order.Confirm();
        order.RecalculateTotal();
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> CancelAsync(long id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id) ?? throw new OrderNotFoundException();
        order.Cancel();
        await _db.SaveChangesAsync();
        return order;
    }

    private async Task<ProductDto?> GetProductAsync(string path)
    {
        var client = _httpClientFactory.CreateClient("catalog-with-correlation");
        try
        {
            var product = await client.GetFromJsonAsync<ProductDto>(path);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve product from catalog at {Path}", path);
            return null;
        }
    }

    private record ProductDto(Guid Id, string Sku, string Name, decimal Price, int Stock);
}
