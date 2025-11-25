namespace PoFastType.Api.Extensions;

/// <summary>
/// Extension methods for HttpContext to reduce code duplication
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Extracts common request context information for logging and diagnostics
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>Request context containing ID, IP, user agent, and timestamp</returns>
    public static RequestContext GetRequestContext(this HttpContext context)
    {
        return new RequestContext
        {
            RequestId = context.TraceIdentifier,
            UserIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Contains common request context information for logging
/// </summary>
public record RequestContext
{
    /// <summary>
    /// Unique request identifier from trace system
    /// </summary>
    public string RequestId { get; init; } = string.Empty;
    
    /// <summary>
    /// Client IP address
    /// </summary>
    public string UserIP { get; init; } = string.Empty;
    
    /// <summary>
    /// Client user agent string
    /// </summary>
    public string UserAgent { get; init; } = string.Empty;
    
    /// <summary>
    /// Request timestamp in UTC
    /// </summary>
    public DateTime Timestamp { get; init; }
}
