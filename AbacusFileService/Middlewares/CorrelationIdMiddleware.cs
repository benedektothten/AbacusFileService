namespace AbacusFileService.Middlewares;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string HeaderKey = "X-Correlation-ID";
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers.ContainsKey(HeaderKey) ? context.Request.Headers[HeaderKey].ToString() : null;
        var correlationId = !string.IsNullOrWhiteSpace(header) ? header : Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderKey] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope("{CorrelationId}", correlationId))
        {
            await _next(context);
        }
    }
}