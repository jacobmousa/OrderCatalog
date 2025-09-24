namespace Orders.Api.Infrastructure;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "x-correlation-id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    { _next = next; _logger = logger; }
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var cid) || string.IsNullOrWhiteSpace(cid))
        {
            cid = Guid.NewGuid().ToString();
        }
        context.TraceIdentifier = cid!;
        context.Response.Headers[HeaderName] = cid!;
        using (_logger.BeginScope(new Dictionary<string, object>{{"CorrelationId", cid!.ToString() }}))
        {
            await _next(context);
        }
    }
}
