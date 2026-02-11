using System.Text.Json;
using ClinicPOS.Application.Common.Exceptions;
using FluentValidation;

namespace ClinicPOS.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, error, message, errors) = ex switch
        {
            DuplicatePhoneException dpe => (
                StatusCodes.Status409Conflict,
                "DuplicatePhoneNumber",
                dpe.Message,
                (object?)dpe.PhoneNumber
            ),
            DuplicateAppointmentException dae => (
                StatusCodes.Status409Conflict,
                "DuplicateAppointment",
                dae.Message,
                (object?)null
            ),
            NotFoundException nfe => (
                StatusCodes.Status404NotFound,
                "NotFound",
                nfe.Message,
                (object?)null
            ),
            ForbiddenException fe => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                fe.Message,
                (object?)null
            ),
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "ValidationFailed",
                "One or more validation errors occurred",
                (object?)ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                        g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            InvalidOperationException ioe => (
                StatusCodes.Status409Conflict,
                "Conflict",
                ioe.Message,
                (object?)null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "InternalError",
                "An unexpected error occurred",
                (object?)null
            )
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception");
        else
            _logger.LogWarning(ex, "Handled exception: {Error}", error);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new Dictionary<string, object?> { ["error"] = error, ["message"] = message };

        if (errors is string detail)
            response["detail"] = detail;
        else if (errors != null)
            response["errors"] = errors;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
