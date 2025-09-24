using System.Net.Http.Json;
using Catalog.Api.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Catalog.Api.Tests;

public class CatalogIntegrationTests : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client;

    public CatalogIntegrationTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Then_Get_Works()
    {
        var create = new CreateProductRequest("ITM-1","Test Item", 12.34m, 5);
        var resp = await _client.PostAsJsonAsync("/api/products", create);
    resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var created = await resp.Content.ReadFromJsonAsync<ProductResponse>();
        created.Should().NotBeNull();

        var get = await _client.GetAsync($"/api/products/{created!.Id}");
    get.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var fetched = await get.Content.ReadFromJsonAsync<ProductResponse>();
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Duplicate_Sku_Returns_BadRequest()
    {
        var create = new CreateProductRequest("ITM-2","Test Item 2", 10m, 1);
        var resp1 = await _client.PostAsJsonAsync("/api/products", create);
    resp1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var resp2 = await _client.PostAsJsonAsync("/api/products", create);
    resp2.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Updates_Resource()
    {
        var create = new CreateProductRequest("ITM-3","Patch Me", 5m, 2);
        var resp = await _client.PostAsJsonAsync("/api/products", create);
        var prod = await resp.Content.ReadFromJsonAsync<ProductResponse>();

        var patch = new UpdateProductRequest(9m, 7);
        var patchResp = await _client.PatchAsJsonAsync($"/api/products/{prod!.Id}", patch);
    patchResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var updated = await patchResp.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Price.Should().Be(9m);
        updated.Stock.Should().Be(7);
    }

    [Fact]
    public async Task Delete_Removes_Resource()
    {
        var create = new CreateProductRequest("ITM-4","Delete Me", 3m, 1);
        var resp = await _client.PostAsJsonAsync("/api/products", create);
        var prod = await resp.Content.ReadFromJsonAsync<ProductResponse>();

        var del = await _client.DeleteAsync($"/api/products/{prod!.Id}");
    del.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/products/{prod.Id}");
    get.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
