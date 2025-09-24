namespace Catalog.Api.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty!;
    public string Name { get; set; } = string.Empty!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
