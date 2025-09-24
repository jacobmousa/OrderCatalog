using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Catalog.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Services;

public interface IProductService
{
    Task<(Product? product, Dictionary<string,string[]>? errors)> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<PagedResponse<ProductResponse>> ListAsync(string? search, decimal? minPrice, decimal? maxPrice, bool? inStockOnly, int page, int pageSize, CancellationToken ct = default);
    Task<Product?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<(Product? product, Dictionary<string,string[]>? errors)> PatchAsync(Guid id, UpdateProductRequest patch, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly ProductDbContext _db;
    private readonly ILogger<ProductService> _logger;
    public ProductService(ProductDbContext db, ILogger<ProductService> logger)
    { _db = db; _logger = logger; }

    public async Task<(Product? product, Dictionary<string,string[]>? errors)> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(request.Sku)) errors[nameof(request.Sku)] = ["SKU is required."];
        if (string.IsNullOrWhiteSpace(request.Name)) errors[nameof(request.Name)] = ["Name is required."];
        if (request.Price < 0) errors[nameof(request.Price)] = ["Price must be >= 0."];
        if (request.Stock < 0) errors[nameof(request.Stock)] = ["Stock must be >= 0."];
        if (errors.Count > 0) return (null, errors);
        if (await _db.Products.AnyAsync(p => p.Sku == request.Sku, ct))
        {
            errors[nameof(request.Sku)] = ["SKU already exists."];
            return (null, errors);
        }
        var product = new Product
        {
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            Price = request.Price,
            Stock = request.Stock
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return (product, null);
    }

    public async Task<PagedResponse<ProductResponse>> ListAsync(string? search, decimal? minPrice, decimal? maxPrice, bool? inStockOnly, int page, int pageSize, CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page; pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var query = _db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }
        if (minPrice is not null) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice is not null) query = query.Where(p => p.Price <= maxPrice.Value);
        if (inStockOnly is true) query = query.Where(p => p.Stock > 0);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ProductResponse.From(p))
            .ToListAsync(ct);
        return new PagedResponse<ProductResponse>(items, total, page, pageSize);
    }

    public async Task<Product?> GetAsync(Guid id, CancellationToken ct = default)
        => await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
        => await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku, ct);

    public async Task<(Product? product, Dictionary<string,string[]>? errors)> PatchAsync(Guid id, UpdateProductRequest patch, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return (null, null); // Not found

        var errors = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
        if (patch.Price is null && patch.Stock is null && patch.Name is null && patch.Sku is null)
            errors["body"] = ["At least one field (price, stock, name, sku) must be provided."];
        if (patch.Price < 0) errors[nameof(patch.Price)] = ["Price must be >= 0."];
        if (patch.Stock < 0) errors[nameof(patch.Stock)] = ["Stock must be >= 0."];
        if (patch.Name is not null && string.IsNullOrWhiteSpace(patch.Name)) errors[nameof(patch.Name)] = ["Name cannot be empty."];
        if (patch.Sku is not null)
        {
            if (string.IsNullOrWhiteSpace(patch.Sku)) errors[nameof(patch.Sku)] = ["Sku cannot be empty."];
            else if (!string.Equals(patch.Sku, product.Sku, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _db.Products.AnyAsync(p => p.Sku == patch.Sku, ct);
                if (exists) errors[nameof(patch.Sku)] = ["SKU already exists."];
            }
        }
        if (errors.Count > 0) return (null, errors);

        if (patch.Price is not null) product.Price = patch.Price.Value;
        if (patch.Stock is not null) product.Stock = patch.Stock.Value;
        if (patch.Name is not null) product.Name = patch.Name.Trim();
        if (patch.Sku is not null && !string.Equals(patch.Sku, product.Sku, StringComparison.OrdinalIgnoreCase)) product.Sku = patch.Sku.Trim();

        product.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (product, null);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return false;
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
