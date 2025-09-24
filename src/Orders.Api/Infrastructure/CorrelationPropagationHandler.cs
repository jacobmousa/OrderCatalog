using System.Net.Http.Headers;

namespace Orders.Api.Infrastructure;

public class CorrelationPropagationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string HeaderName = "x-correlation-id";
    public CorrelationPropagationHandler(IHttpContextAccessor accessor) => _httpContextAccessor = accessor;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx != null && ctx.Request.Headers.TryGetValue(HeaderName, out var cid) && !string.IsNullOrWhiteSpace(cid))
        {
            if (!request.Headers.Contains(HeaderName)) request.Headers.Add(HeaderName, (IEnumerable<string>)cid);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
