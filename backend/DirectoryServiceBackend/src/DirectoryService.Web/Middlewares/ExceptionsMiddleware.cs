using DirectoryService.Domain.shared;
using DirectoryService.Domain.shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace DirectoryService.Web.Middlewares;

public class ExceptionsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionsMiddleware> _logger;
    public ExceptionsMiddleware(RequestDelegate next, ILogger<ExceptionsMiddleware> logger)
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
            await HandlerExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }
    private Task HandlerExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, exception.Message);
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        (int code, Errors errors) = exception switch
        {
            BadRequestException => (
                StatusCodes.Status400BadRequest,
                TryDeserializeErrors(exception.Message, out var errs)
                    ? errs
                    : Error.Validation("bad.request", exception.Message).ToErrors()
            ),

            NotFoundException => (
                StatusCodes.Status404NotFound,
                TryDeserializeErrors(exception.Message, out var errs)
                    ? errs
                    : Error.NotFound("record.not.found", exception.Message).ToErrors()
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                Error.Failure("server.error", exception.Message).ToErrors()
            )
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = code;
        return context.Response.WriteAsJsonAsync(errors);
    }

    private static bool TryDeserializeErrors(string message, out Errors errors)
    {
        errors = null!;
        if (string.IsNullOrWhiteSpace(message))
            return false;

        // Быстрая проверка: JSON-массив всегда начинается с '['
        string trimmed = message.Trim();
        if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
            return false;

        try
        {
            // Пробуем десериализовать в список (implicit operator превратит List<Error> в Errors)
            var list = JsonSerializer.Deserialize<List<Error>>(trimmed);
            if (list != null)
            {
                errors = list; // Использует implicit operator Errors(List<Error>)
                return true;
            }
        }
        catch (JsonException)
        {
            // Если это была строка, похожая на JSON, но не валидная
            return false;
        }

        return false;
    }
}

public static class ExceptionMiddlewareExtension
{
    public static IApplicationBuilder UseExceptionMiddleware(this WebApplication app) =>
        app.UseMiddleware<ExceptionsMiddleware>();
}
