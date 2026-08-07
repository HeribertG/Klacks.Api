// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Logging;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Exceptions;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        this.next = next;
        this._logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "FluentValidation ValidationException caught by middleware");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "One or more validation failures occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Please refer to the errors property for additional details."
            };

            problem.Extensions.Add("errors", ex.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));

            await context.Response.WriteAsJsonAsync(problem);
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
            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (ContainerLockedException ex)
        {
            _logger.LogWarning(ex, "ContainerLockedException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status423Locked;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Container Locked",
                Status = StatusCodes.Status423Locked,
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (AutofillRunConflictException ex)
        {
            _logger.LogInformation(
                "Autofill run conflict: {Family} for the same selection is already running as {JobId}",
                ex.Family, ex.RunningJobId);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
            };
            // The client attaches to the running job instead of starting a competing one, so it must be
            // able to tell this 409 apart from every other conflict.
            problem.Extensions["errorCode"] = AutofillLimits.RunConflictErrorCode;
            problem.Extensions["runningJobId"] = ex.RunningJobId;

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (PeriodValidationConflictException ex)
        {
            _logger.LogWarning(
                ex, "PeriodValidationConflictException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
            };
            // The client re-confirms against the count it was shown, so the figure has to travel as a
            // field. Parsing it back out of the message would break on the first wording change.
            problem.Extensions["errorCode"] = PeriodValidationConflictException.ErrorCode;
            problem.Extensions["currentErrorCount"] = ex.CurrentErrorCount;

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "ConflictException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (StaleWizardResultException ex)
        {
            _logger.LogWarning(ex, "StaleWizardResultException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
            };
            // The client must tell a stale wizard result apart from any other 409 to offer a re-run.
            problem.Extensions["errorCode"] = "staleWizardResult";
            problem.Extensions["expectedWorkCount"] = ex.ExpectedWorkCount;
            problem.Extensions["actualWorkCount"] = ex.ActualWorkCount;
            problem.Extensions["expectedBreakCount"] = ex.ExpectedBreakCount;
            problem.Extensions["actualBreakCount"] = ex.ActualBreakCount;
            problem.Extensions["placementChanged"] = ex.PlacementChanged;

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "UnauthorizedException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "NotFoundException caught by middleware: {Message}", ex.Message);
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
        catch (AutofillLimitExceededException ex)
        {
            _logger.LogInformation(
                "Autofill run refused: {Family} with {Agents} agents, {Shifts} shifts, {Days} days",
                ex.Family, ex.Agents, ex.Shifts, ex.PeriodDays);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
            };
            // The dialog shows which limit was hit and by how much, so it needs the figures, not prose.
            // "code" is what the existing clients read; "errorCode" is the convention the newer error
            // bodies use. Emitting both keeps the limit toast working without a lockstep deployment.
            problem.Extensions["errorCode"] = ex.Code;
            problem.Extensions["code"] = ex.Code;
            problem.Extensions["agents"] = ex.Agents;
            problem.Extensions["shifts"] = ex.Shifts;
            problem.Extensions["periodDays"] = ex.PeriodDays;
            problem.Extensions["maxAgents"] = ex.MaxAgents;
            problem.Extensions["maxShifts"] = ex.MaxShifts;
            problem.Extensions["maxPeriodDays"] = ex.MaxPeriodDays;
            problem.Extensions["slotProduct"] = ex.SlotProduct;
            problem.Extensions["maxSlotProduct"] = ex.MaxSlotProduct;

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (BadRequestException ex)
        {
            _logger.LogWarning(ex, "BadRequestException caught by middleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Request cancelled: {Method} {Path}", context.Request.Method.ForLog(), context.Request.Path.ToString().ForLog());
            context.Response.StatusCode = 499;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "57014")
        {
            _logger.LogDebug("PostgreSQL query cancelled: {Method} {Path}", context.Request.Method.ForLog(), context.Request.Path.ToString().ForLog());
            context.Response.StatusCode = 499;
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