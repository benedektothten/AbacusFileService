using System.Diagnostics;
using AbacusFileService.Providers;

namespace AbacusFileService.Middlewares
{
    public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = CorrelationIdProvider.GetOrCreate(context);

            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    await next(context);
                }
                finally
                {
                    sw.Stop();

                    var method = context.Request.Method;
                    var path = context.Request.Path + (context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty);
                    var statusCode = context.Response?.StatusCode;
                    var requestLength = context.Request.ContentLength?.ToString() ?? "unknown";
                    var responseLength = context.Response?.ContentLength?.ToString() ?? "unknown";
                    var elapsedMs = sw.Elapsed.TotalMilliseconds;

                    logger.LogInformation(
                        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms - RequestLength: {RequestLength} bytes, ResponseLength: {ResponseLength} bytes",
                        method, path, statusCode, elapsedMs, requestLength, responseLength);
                }
            }
        }
    }
}
