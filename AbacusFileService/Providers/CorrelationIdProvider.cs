using Microsoft.Extensions.Primitives;

namespace AbacusFileService.Providers;

public static class CorrelationIdProvider
{
    private const string HeaderKey = "X-Correlation-ID";
    private const string ItemsKey = "CorrelationId";

    public static string GetOrCreate(HttpContext context)
    {
        // Prefer an explicit incoming header
        if (context.Request.Headers.TryGetValue(HeaderKey, out StringValues header) && !StringValues.IsNullOrEmpty(header))
        {
            var id = header.ToString();
            context.Items[ItemsKey] = id;
            context.TraceIdentifier = id;
            return id;
        }

        // Use an existing item if already set by earlier middleware
        if (context.Items[ItemsKey] is string existing && !string.IsNullOrWhiteSpace(existing))
        {
            context.TraceIdentifier = existing;
            return existing;
        }

        // When no header/item exists, generate a new GUID and set it so it's visible everywhere.
        var newId = Guid.NewGuid().ToString();
        context.Items[ItemsKey] = newId;
        context.TraceIdentifier = newId;
        return newId;
    }
}