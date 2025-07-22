using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Exceptions;

public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ValidationExceptionMiddleware> _logger;

    public ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
    {
        this.next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidRequestException ex)
        {
            _logger.LogWarning(ex, "InvalidRequestException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Invalid request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "KeyNotFoundException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DbUpdateException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Database Update Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "A database error occurred. Please check your input." // Generic message for client
            };

            // In development, you might want to expose more details
            if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                problem.Detail = ex.InnerException?.Message ?? ex.Message;
            }

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "An unexpected error occurred",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An internal server error has occurred." // Generic message for client
            };

            // In development, you might want to expose more details
            if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                problem.Detail = ex.Message;
                problem.Extensions.Add("stackTrace", ex.StackTrace);
            }

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}