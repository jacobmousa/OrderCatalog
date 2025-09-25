using Orders.Api.Entities;

namespace Orders.Api.Dtos;

public record CreateOrderRequest(string CustomerId);
public record AddOrderItemRequest(Guid? ProductId, string? Sku, int Qty);
public record OrderItemResponse(Guid ProductId, string Sku, int Qty, decimal UnitPrice, decimal LineTotal)
{ public static OrderItemResponse From(OrderItem i) => new(i.ProductId, i.Sku, i.Qty, i.UnitPrice, i.UnitPrice * i.Qty); }
public record OrderResponse(long Id, string CustomerId, string Status, decimal TotalAmount, DateTime CreatedUtc, DateTime UpdatedUtc, IReadOnlyList<OrderItemResponse> Items)
{ public static OrderResponse From(Order o) => new(o.Id, o.CustomerId, o.Status.ToString(), o.TotalAmount, o.CreatedUtc, o.UpdatedUtc, o.Items.Select(OrderItemResponse.From).ToList()); }
public record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
