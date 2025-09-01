using System.Net;
using System.Text.Json;
using Serilog;

namespace PoFastType.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception occurred. RequestPath: {RequestPath}, Method: {Method}, UserAgent: {UserAgent}", 
                context.Request.Path, 
                context.Request.Method, 
                context.Request.Headers["User-Agent"].ToString());

            _logger.LogError(ex, "Unhandled exception occurred processing request {RequestPath}", context.Request.Path);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = new
            {
                message = "An internal server error occurred.",
                details = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() 
                    ? exception.Message 
                    : "Please try again later."
            }
        };

        context.Response.StatusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            NotImplementedException => (int)HttpStatusCode.NotImplemented,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
