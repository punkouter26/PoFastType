using Serilog;
using System.Text;

namespace PoFastType.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        
        // Log incoming request
        Log.Information("Request {RequestId} started: {Method} {Path} from {RemoteIP} with UserAgent: {UserAgent}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers["User-Agent"].ToString());

        // Log request body for POST/PUT requests (for game actions and state changes)
        if (context.Request.Method is "POST" or "PUT" && 
            context.Request.ContentType?.Contains("application/json") == true)
        {
            context.Request.EnableBuffering();
            var bodyAsText = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;
            
            Log.Information("Request {RequestId} body: {RequestBody}", requestId, bodyAsText);
        }

        var startTime = DateTime.UtcNow;
        
        try
        {
            await _next(context);
            
            var duration = DateTime.UtcNow - startTime;
            
            // Log response
            Log.Information("Request {RequestId} completed: {StatusCode} in {Duration}ms",
                requestId,
                context.Response.StatusCode,
                duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            Log.Error(ex, "Request {RequestId} failed after {Duration}ms",
                requestId,
                duration.TotalMilliseconds);
            
            throw;
        }
    }
}
