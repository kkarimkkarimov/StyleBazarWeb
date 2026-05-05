using System.Net;
using System.Text.Json;
using Stylebazar.BLL.Exceptions;

namespace Stylebazar.API.Middlewares;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        (int statusCode, object payload) = exception switch
        {
            ValidationException ve => (
                (int)HttpStatusCode.BadRequest,
                (object)new { message = ve.Message, errors = ve.Errors }),

            NotFoundException nfe => (
                (int)HttpStatusCode.NotFound,
                (object)new { message = nfe.Message }),

            BusinessException be => (
                (int)HttpStatusCode.UnprocessableEntity,
                (object)new { message = be.Message }),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                (object)new { message = "An unexpected error occurred." })
        };

        if (statusCode == (int)HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning(exception, "Handled exception: {ExceptionType}", exception.GetType().Name);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
