using Catalog.Api.Dtos;
using Catalog.Api.Entities;
using Catalog.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    public ProductsController(IProductService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var (product, errors) = await _service.CreateAsync(request);
        if (errors is not null)
            return ValidationProblem(new ValidationProblemDetails(errors));
        var response = ProductResponse.From(product!);
        return CreatedAtRoute("GetProductById", new { id = product!.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? inStockOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var pageResult = await _service.ListAsync(search, minPrice, maxPrice, inStockOnly, page, pageSize);
        return Ok(pageResult);
    }

    [HttpGet("{id:guid}", Name = "GetProductById")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
    var product = await _service.GetAsync(id);
        return product is null ? NotFound() : Ok(ProductResponse.From(product));
    }

    [HttpGet("sku/{sku}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySku(string sku)
    {
    var product = await _service.GetBySkuAsync(sku);
        return product is null ? NotFound() : Ok(ProductResponse.From(product));
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] UpdateProductRequest patch)
    {
        var (product, errors) = await _service.PatchAsync(id, patch);
        if (product is null && errors is null) return NotFound();
        if (errors is not null) return ValidationProblem(new ValidationProblemDetails(errors));
        return Ok(ProductResponse.From(product!));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var removed = await _service.DeleteAsync(id);
        return removed ? NoContent() : NotFound();
    }
}
