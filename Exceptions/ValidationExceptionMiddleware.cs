using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Exceptions;

public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate next;

    public ValidationExceptionMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidRequestException ex)
        {
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
    }
}
