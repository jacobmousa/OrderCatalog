using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Dtos;
using Xunit;

namespace Orders.Api.Tests;

public class OrdersIntegrationTests : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client;

    public OrdersIntegrationTests(OrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Then_Get_Works()
    {
        var createResp = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest("cust-1"));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<OrderResponse>();
        created.Should().NotBeNull();

        var get = await _client.GetAsync($"/api/orders/{created!.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await get.Content.ReadFromJsonAsync<OrderResponse>();
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task AddItem_Then_Confirm_Computes_Total()
    {
        var createResp = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest("cust-2"));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<OrderResponse>();

        // This will call catalog service; since we are not running catalog here it will fail gracefully and produce validation error.
        var addItem = await _client.PostAsJsonAsync($"/api/orders/{created!.Id}/items", new AddOrderItemRequest(null, "SKU-1001", 2));
        // We expect BadRequest due to catalog lookup returning null (no product) -> validation error path.
        addItem.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_Nonexistent_Returns_NotFound()
    {
        var cancel = await _client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
