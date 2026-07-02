using System.Text.Json;
using TeamFlow.Core.Common;
 
namespace TeamFlow.API.Middlewares;
 
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
 
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }
 
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
 
        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (401, exception.Message),
            KeyNotFoundException => (404, exception.Message),
            InvalidOperationException => (400, exception.Message),
            _ => (500, "خطای داخلی سرور. لطفاً دوباره تلاش کنید.")
        };
 
        context.Response.StatusCode = statusCode;
 
        var response = ApiResponse.Fail(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
 
        await context.Response.WriteAsync(json);
    }
}