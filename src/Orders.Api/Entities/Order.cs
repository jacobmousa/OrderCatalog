namespace Orders.Api.Entities;

public class Order
{
    public long Id { get; set; }
    public string CustomerId { get; set; } = string.Empty!;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public void RecalculateTotal() => TotalAmount = Items.Sum(i => i.UnitPrice * i.Qty);
    public void EnsureDraft()
    { if (Status != OrderStatus.Draft) throw new ValidationException("status", "Only draft orders can be modified."); }
    public void Confirm()
    { EnsureDraft(); if (Items.Count == 0) throw new ValidationException("items", "Cannot confirm order without items."); Status = OrderStatus.Confirmed; UpdatedUtc = DateTime.UtcNow; }
    public void Cancel()
    { if (Status == OrderStatus.Cancelled) return; if (Status == OrderStatus.Confirmed) throw new ValidationException("status", "Confirmed orders cannot be cancelled (demo rule)." ); Status = OrderStatus.Cancelled; UpdatedUtc = DateTime.UtcNow; }
}

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty!;
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
}

public enum OrderStatus { Draft, Confirmed, Cancelled }
