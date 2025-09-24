namespace Catalog.Api.Products;

public record CreateProductRequest(string Sku, string Name, decimal Price, int Stock);
public record UpdateProductRequest(decimal? Price, int? Stock);
public record ProductResponse(Guid Id, string Sku, string Name, decimal Price, int Stock, DateTime CreatedUtc, DateTime UpdatedUtc)
{
    public static ProductResponse From(Product p) => new(p.Id, p.Sku, p.Name, p.Price, p.Stock, p.CreatedUtc, p.UpdatedUtc);
}

public record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
