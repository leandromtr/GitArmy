using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GitArmy.API.Handlers;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ValidationProblemDetails
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            }, cancellationToken);
            return true;
        }

        if (exception is InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = exception.Message,
                Status = StatusCodes.Status404NotFound
            }, cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Internal server error",
            Status = StatusCodes.Status500InternalServerError
        }, cancellationToken);
        return true;
    }
}
