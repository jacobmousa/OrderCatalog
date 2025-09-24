using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Dtos;
using Orders.Api.Entities;
using Orders.Api.Exceptions;
using Orders.Api.Infrastructure;
using Orders.Api.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IOrderService _service;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderDbContext db, IOrderService service, ILogger<OrdersController> logger)
    { _db = db; _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        try
        {
            if (request is null)
            {
                // Should not happen with proper JSON body; log explicitly if it does.
                _logger.LogWarning("CreateOrderRequest model binding produced null request body.");
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    {"body", new[]{"Request body was empty or malformed JSON."}} }
                ));
            }
            var order = await _service.CreateDraftAsync(request.CustomerId);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, OrderResponse.From(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create draft order for customer {CustomerId}", request?.CustomerId);
            throw; // Let global handler produce ProblemDetails (500) with correlation id
        }
    }

    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddOrderItemRequest request)
    {
        try
        {
            var order = await _service.AddItemAsync(id, request);
            return Ok(OrderResponse.From(order));
        }
        catch (OrderNotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException vex)
        {
            var errors = new Dictionary<string, string[]> { { vex.Field, new[] { vex.Message } } };
            return ValidationProblem(new ValidationProblemDetails(errors));
        }
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        try
        {
            var order = await _service.ConfirmAsync(id);
            return Ok(OrderResponse.From(order));
        }
        catch (OrderNotFoundException) { return NotFound(); }
        catch (ValidationException vex)
        {
            var errors = new Dictionary<string, string[]> { { vex.Field, new[] { vex.Message } } };
            return ValidationProblem(new ValidationProblemDetails(errors));
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var order = await _service.CancelAsync(id);
            return Ok(OrderResponse.From(order));
        }
        catch (OrderNotFoundException) { return NotFound(); }
        catch (ValidationException vex)
        {
            var errors = new Dictionary<string, string[]> { { vex.Field, new[] { vex.Message } } };
            return ValidationProblem(new ValidationProblemDetails(errors));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        return Ok(OrderResponse.From(order));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page; pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var st))
            query = query.Where(o => o.Status == st);
        if (!string.IsNullOrWhiteSpace(customerId)) query = query.Where(o => o.CustomerId == customerId);
        var total = await query.CountAsync();
        var list = await query.OrderByDescending(o => o.CreatedUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => OrderResponse.From(o)).ToListAsync();
        return Ok(new PagedResponse<OrderResponse>(list, total, page, pageSize));
    }
}
