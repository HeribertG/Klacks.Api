// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Logging;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Exceptions;

/// <summary>
/// Turns every exception that escapes the request pipeline into a problem+json response (or a bare 499 for a cancelled
/// request). The exception types are matched in a fixed order - a subclass always ahead of its base - and each type has
/// its own handler that logs it and writes its status, title, detail and extension fields. UnauthorizedException maps to
/// 401 by design; a missing right is a ForbiddenException (403).
/// </summary>
/// <param name="next">The rest of the request pipeline</param>
/// <param name="logger">Logs each mapped exception at the level of its handler</param>
public class ErrorHandlingMiddleware
{
    private const string ProblemJsonContentType = "application/problem+json";
    private const int ClientClosedRequestStatus = 499;
    private const string QueryCanceledSqlState = "57014";

    private const string ErrorCodeKey = "errorCode";
    private const string CodeKey = "code";
    private const string ErrorsKey = "errors";
    private const string StackTraceKey = "stackTrace";
    private const string StaleWizardResultErrorCode = "staleWizardResult";
    private const string ReasonsKey = "reasons";
    private const string RunningJobIdKey = "runningJobId";
    private const string CurrentErrorCountKey = "currentErrorCount";
    private const string BlockersKey = "blockers";
    private const string BlockerTotalKey = "blockerTotal";
    private const string ExpectedWorkCountKey = "expectedWorkCount";
    private const string ActualWorkCountKey = "actualWorkCount";
    private const string ExpectedBreakCountKey = "expectedBreakCount";
    private const string ActualBreakCountKey = "actualBreakCount";
    private const string PlacementChangedKey = "placementChanged";
    private const string AgentsKey = "agents";
    private const string ShiftsKey = "shifts";
    private const string PeriodDaysKey = "periodDays";
    private const string MaxAgentsKey = "maxAgents";
    private const string MaxShiftsKey = "maxShifts";
    private const string MaxPeriodDaysKey = "maxPeriodDays";
    private const string SlotProductKey = "slotProduct";
    private const string MaxSlotProductKey = "maxSlotProduct";

    private const string BadRequestTitle = "Bad Request";
    private const string ConflictTitle = "Conflict";
    private const string NotFoundTitle = "Not Found";
    private const string InvalidRequestTitle = "Invalid request";
    private const string ValidationTitle = "One or more validation failures occurred.";
    private const string ValidationDetail = "Please refer to the errors property for additional details.";
    private const string DatabaseUpdateTitle = "Database Update Error";
    private const string DatabaseUpdateDetail = "A database error occurred. Please check your input.";
    private const string DuplicateEntryDetail = "A duplicate entry already exists.";
    private const string ReferencedDataDetail = "Referenced data not found or constraint violation.";
    private const string DatabaseConstraintDetail = "Database constraint violation.";
    private const string ContainerLockedTitle = "Container Locked";
    private const string UnprocessableEntityTitle = "Unprocessable Entity";
    private const string ForbiddenTitle = "Forbidden";
    private const string UnauthorizedTitle = "Unauthorized";
    private const string UnexpectedErrorTitle = "An unexpected error occurred";
    private const string UnexpectedErrorDetail = "An internal server error has occurred.";

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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// First match wins, exactly like an ordered catch list: every subclass arm stands before its base arm
    /// (AccountRegistrationRejected/ClientImportRejected before InvalidRequest; ClientImportConflict, AutofillRunConflict,
    /// PeriodValidationConflict and PayrollExportBlocked before Conflict; AutofillLimitExceeded before BadRequest).
    /// </summary>
    private Task HandleExceptionAsync(HttpContext context, Exception exception) => exception switch
    {
        ValidationException ex => HandleValidationAsync(context, ex),
        AccountRegistrationRejectedException ex => HandleAccountRegistrationRejectedAsync(context, ex),
        ClientImportRejectedException ex => HandleClientImportRejectedAsync(context, ex),
        ClientImportConflictException ex => HandleClientImportConflictAsync(context, ex),
        InvalidRequestException ex => HandleInvalidRequestAsync(context, ex),
        KeyNotFoundException ex => HandleKeyNotFoundAsync(context, ex),
        DbUpdateException ex => HandleDbUpdateAsync(context, ex),
        DatabaseUpdateException ex => HandleDatabaseUpdateAsync(context, ex),
        ConcurrencyException ex => HandleConcurrencyAsync(context, ex),
        ContainerLockedException ex => HandleContainerLockedAsync(context, ex),
        AutofillRunConflictException ex => HandleAutofillRunConflictAsync(context, ex),
        PlanningRuleConfigurationException ex => HandlePlanningRuleConfigurationAsync(context, ex),
        PeriodValidationConflictException ex => HandlePeriodValidationConflictAsync(context, ex),
        PayrollExportBlockedException ex => HandlePayrollExportBlockedAsync(context, ex),
        ConflictException ex => HandleConflictAsync(context, ex),
        StaleWizardResultException ex => HandleStaleWizardResultAsync(context, ex),
        ForbiddenException ex => HandleForbiddenAsync(context, ex),
        UnauthorizedException ex => HandleUnauthorizedAsync(context, ex),
        NotFoundException ex => HandleNotFoundAsync(context, ex),
        AutofillLimitExceededException ex => HandleAutofillLimitExceededAsync(context, ex),
        BadRequestException ex => HandleBadRequestAsync(context, ex),
        OperationCanceledException => HandleRequestCancelledAsync(context),
        Npgsql.PostgresException { SqlState: QueryCanceledSqlState } => HandleQueryCancelledAsync(context),
        _ => HandleUnexpectedAsync(context, exception),
    };

    private async Task HandleValidationAsync(HttpContext context, ValidationException ex)
    {
        _logger.LogWarning(ex, "FluentValidation ValidationException caught by middleware");
        var problem = StartProblem(context, StatusCodes.Status400BadRequest, ValidationTitle, ValidationDetail);
        problem.Extensions.Add(ErrorsKey, ex.Errors.GroupBy(x => x.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));

        await context.Response.WriteAsJsonAsync(problem);
    }

    private async Task HandleAccountRegistrationRejectedAsync(HttpContext context, AccountRegistrationRejectedException ex)
    {
        _logger.LogWarning("AccountRegistrationRejectedException caught by middleware: {Reasons}", string.Join(',', ex.Reasons));
        var problem = StartProblem(context, StatusCodes.Status400BadRequest, BadRequestTitle, ex.Message);
        problem.Extensions[ErrorCodeKey] = AccountRegistrationErrorCodes.ErrorCode;
        problem.Extensions[ReasonsKey] = ex.Reasons;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private async Task HandleClientImportRejectedAsync(HttpContext context, ClientImportRejectedException ex)
    {
        _logger.LogWarning(ex, "ClientImportRejectedException caught by middleware: {Code}", ex.Code);
        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, BadRequestTitle, ex.Message, ex.Code);
    }

    private async Task HandleClientImportConflictAsync(HttpContext context, ClientImportConflictException ex)
    {
        _logger.LogWarning(ex, "ClientImportConflictException caught by middleware: {Code}", ex.Code);
        await WriteProblemAsync(context, StatusCodes.Status409Conflict, ConflictTitle, ex.Message, ex.Code);
    }

    private async Task HandleInvalidRequestAsync(HttpContext context, InvalidRequestException ex)
    {
        _logger.LogWarning(ex, "InvalidRequestException caught by middleware: {Message}", ex.Message);
        if (ex is ICodedRejection coded)
        {
            await WriteCodedProblemAsync(context, StatusCodes.Status400BadRequest, InvalidRequestTitle, ex.Message, coded);
        }
        else
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, InvalidRequestTitle, ex.Message);
        }
    }

    private async Task HandleKeyNotFoundAsync(HttpContext context, KeyNotFoundException ex)
    {
        _logger.LogWarning(ex, "KeyNotFoundException caught by middleware: {Message}", ex.Message);
        await WriteProblemAsync(context, StatusCodes.Status404NotFound, NotFoundTitle, ex.Message);
    }

    private async Task HandleDbUpdateAsync(HttpContext context, DbUpdateException ex)
    {
        _logger.LogError(ex, "DbUpdateException caught by middleware: {Message}", ex.Message);
        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, DatabaseUpdateTitle, DatabaseUpdateDetail);
    }

    /// <summary>
    /// Answers a Domain DatabaseUpdateException that no BaseHandler translated, with the status and wording BaseHandler
    /// uses for it (400 invalid request); the operation name and the raw database message stay out of the response.
    /// </summary>
    private async Task HandleDatabaseUpdateAsync(HttpContext context, DatabaseUpdateException ex)
    {
        _logger.LogError(ex, "DatabaseUpdateException caught by middleware: {Message}", ex.Message);
        var detail = ex.IsDuplicate ? DuplicateEntryDetail
            : ex.IsForeignKeyViolation ? ReferencedDataDetail
            : DatabaseConstraintDetail;
        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, InvalidRequestTitle, detail);
    }

    private async Task HandleConcurrencyAsync(HttpContext context, ConcurrencyException ex)
    {
        _logger.LogError(ex, "ConcurrencyException caught by middleware: {Message}", ex.Message);
        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, InvalidRequestTitle, ex.Message);
    }

    private async Task HandleContainerLockedAsync(HttpContext context, ContainerLockedException ex)
    {
        _logger.LogWarning(ex, "ContainerLockedException caught by middleware: {Message}", ex.Message);
        await WriteProblemAsync(context, StatusCodes.Status423Locked, ContainerLockedTitle, ex.Message);
    }

    /// <summary>
    /// The client attaches to the running job instead of starting a competing one, so it must be able to tell this 409
    /// apart from every other conflict: errorCode plus the running job id.
    /// </summary>
    private async Task HandleAutofillRunConflictAsync(HttpContext context, AutofillRunConflictException ex)
    {
        _logger.LogInformation(
            "Autofill run conflict: {Family} for the same selection is already running as {JobId}",
            ex.Family, ex.RunningJobId);
        var problem = StartProblem(context, StatusCodes.Status409Conflict, ConflictTitle, ex.Message);
        problem.Extensions[ErrorCodeKey] = AutofillLimits.RunConflictErrorCode;
        problem.Extensions[RunningJobIdKey] = ex.RunningJobId;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private async Task HandlePlanningRuleConfigurationAsync(HttpContext context, PlanningRuleConfigurationException ex)
    {
        _logger.LogError(ex, "PlanningRuleConfigurationException caught by middleware: {ConstraintId}", ex.ConstraintId);
        await WriteProblemAsync(context, StatusCodes.Status422UnprocessableEntity, UnprocessableEntityTitle, ex.Message, PlanningRuleConfigurationException.ErrorCode);
    }

    /// <summary>
    /// The client re-confirms against the count it was shown, so the figure has to travel as a field. Parsing it back out
    /// of the message would break on the first wording change.
    /// </summary>
    private async Task HandlePeriodValidationConflictAsync(HttpContext context, PeriodValidationConflictException ex)
    {
        _logger.LogWarning(
            ex, "PeriodValidationConflictException caught by middleware: {Message}", ex.Message);
        var problem = StartProblem(context, StatusCodes.Status409Conflict, ConflictTitle, ex.Message);
        problem.Extensions[ErrorCodeKey] = PeriodValidationConflictException.ErrorCode;
        problem.Extensions[CurrentErrorCountKey] = ex.CurrentErrorCount;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private async Task HandlePayrollExportBlockedAsync(HttpContext context, PayrollExportBlockedException ex)
    {
        _logger.LogWarning(ex, "PayrollExportBlockedException caught by middleware: {BlockerTotal} blockers", ex.Completeness.BlockerTotal);
        var problem = StartProblem(context, StatusCodes.Status409Conflict, ConflictTitle, ex.Message);
        problem.Extensions[ErrorCodeKey] = PayrollExportErrorCodes.Blocked;
        problem.Extensions[BlockersKey] = ex.Completeness.Blockers;
        problem.Extensions[BlockerTotalKey] = ex.Completeness.BlockerTotal;

        await context.Response.WriteAsJsonAsync(problem);
    }

    /// <summary>
    /// A conflict answered with 409. A coded conflict writes its detail fields first and its code last, so no detail field
    /// can overwrite "errorCode" (the same order as the coded 400 body).
    /// </summary>
    private async Task HandleConflictAsync(HttpContext context, ConflictException ex)
    {
        _logger.LogWarning(ex, "ConflictException caught by middleware: {Message}", ex.Message);
        var problem = StartProblem(context, StatusCodes.Status409Conflict, ConflictTitle, ex.Message);

        if (ex.ConflictCode is { } conflictCode)
        {
            foreach (var (field, value) in ex.ConflictDetails ?? new Dictionary<string, object?>())
            {
                problem.Extensions[field] = value;
            }

            problem.Extensions[ErrorCodeKey] = conflictCode;
        }

        await context.Response.WriteAsJsonAsync(problem);
    }

    /// <summary>
    /// The client must tell a stale wizard result apart from any other 409 to offer a re-run, so the code and the
    /// expected/actual counts travel as fields.
    /// </summary>
    private async Task HandleStaleWizardResultAsync(HttpContext context, StaleWizardResultException ex)
    {
        _logger.LogWarning(ex, "StaleWizardResultException caught by middleware: {Message}", ex.Message);
        var problem = StartProblem(context, StatusCodes.Status409Conflict, ConflictTitle, ex.Message);
        problem.Extensions[ErrorCodeKey] = StaleWizardResultErrorCode;
        problem.Extensions[ExpectedWorkCountKey] = ex.ExpectedWorkCount;
        problem.Extensions[ActualWorkCountKey] = ex.ActualWorkCount;
        problem.Extensions[ExpectedBreakCountKey] = ex.ExpectedBreakCount;
        problem.Extensions[ActualBreakCountKey] = ex.ActualBreakCount;
        problem.Extensions[PlacementChangedKey] = ex.PlacementChanged;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private async Task HandleForbiddenAsync(HttpContext context, ForbiddenException ex)
    {
        _logger.LogWarning(ex, "ForbiddenException caught by middleware: {Message}", ex.Message);
        await WriteProblemAsync(context, StatusCodes.Status403Forbidden, ForbiddenTitle, ex.Message);
    }

    private async Task HandleUnauthorizedAsync(HttpContext context, UnauthorizedException ex)
    {
        _logger.LogWarning(ex, "UnauthorizedException caught by middleware: {Message}", ex.Message);
        await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, UnauthorizedTitle, ex.Message);
    }

    private async Task HandleNotFoundAsync(HttpContext context, NotFoundException ex)
    {
        _logger.LogWarning(ex, "NotFoundException caught by middleware: {Message}", ex.Message);
        await WriteProblemAsync(context, StatusCodes.Status404NotFound, NotFoundTitle, ex.Message);
    }

    /// <summary>
    /// The dialog shows which limit was hit and by how much, so it needs the figures, not prose. "code" is what the
    /// existing clients read; "errorCode" is the convention the newer error bodies use. Emitting both keeps the limit
    /// toast working without a lockstep deployment.
    /// </summary>
    private async Task HandleAutofillLimitExceededAsync(HttpContext context, AutofillLimitExceededException ex)
    {
        _logger.LogInformation(
            "Autofill run refused: {Family} with {Agents} agents, {Shifts} shifts, {Days} days",
            ex.Family, ex.Agents, ex.Shifts, ex.PeriodDays);
        var problem = StartProblem(context, StatusCodes.Status400BadRequest, BadRequestTitle, ex.Message);
        problem.Extensions[ErrorCodeKey] = ex.Code;
        problem.Extensions[CodeKey] = ex.Code;
        problem.Extensions[AgentsKey] = ex.Agents;
        problem.Extensions[ShiftsKey] = ex.Shifts;
        problem.Extensions[PeriodDaysKey] = ex.PeriodDays;
        problem.Extensions[MaxAgentsKey] = ex.MaxAgents;
        problem.Extensions[MaxShiftsKey] = ex.MaxShifts;
        problem.Extensions[MaxPeriodDaysKey] = ex.MaxPeriodDays;
        problem.Extensions[SlotProductKey] = ex.SlotProduct;
        problem.Extensions[MaxSlotProductKey] = ex.MaxSlotProduct;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private async Task HandleBadRequestAsync(HttpContext context, BadRequestException ex)
    {
        _logger.LogWarning(ex, "BadRequestException caught by middleware: {Message}", ex.Message);
        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, BadRequestTitle, ex.Message);
    }

    /// <summary>
    /// A cancelled request gets the status alone: no content type, no body.
    /// </summary>
    private Task HandleRequestCancelledAsync(HttpContext context)
    {
        _logger.LogDebug("Request cancelled: {Method} {Path}", context.Request.Method.ForLog(), context.Request.Path.ToString().ForLog());
        context.Response.StatusCode = ClientClosedRequestStatus;
        return Task.CompletedTask;
    }

    /// <summary>
    /// PostgreSQL reports a cancelled statement (SQLSTATE 57014) as its own exception; it is answered like a cancelled
    /// request. Every other PostgresException falls through to the unexpected-error handler.
    /// </summary>
    private Task HandleQueryCancelledAsync(HttpContext context)
    {
        _logger.LogDebug("PostgreSQL query cancelled: {Method} {Path}", context.Request.Method.ForLog(), context.Request.Path.ToString().ForLog());
        context.Response.StatusCode = ClientClosedRequestStatus;
        return Task.CompletedTask;
    }

    /// <summary>
    /// The client gets a generic message; only in Development the exception message and stack trace are exposed.
    /// Status and content type are set before the environment is resolved, as they always were.
    /// </summary>
    private async Task HandleUnexpectedAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
        var problem = StartProblem(context, StatusCodes.Status500InternalServerError, UnexpectedErrorTitle, UnexpectedErrorDetail);

        if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            problem.Detail = ex.Message;
            problem.Extensions.Add(StackTraceKey, ex.StackTrace);
        }

        await context.Response.WriteAsJsonAsync(problem);
    }

    /// <summary>
    /// A coded rejection answered with a non-409 status: the code goes out as "errorCode" and "code" (the two names
    /// the existing 400 bodies use) and every detail field next to it; status, title and detail stay as for the
    /// uncoded rejection.
    /// </summary>
    private static async Task WriteCodedProblemAsync(HttpContext context, int status, string title, string detail, ICodedRejection rejection)
    {
        var problem = StartProblem(context, status, title, detail);

        foreach (var (field, value) in rejection.Details)
        {
            problem.Extensions[field] = value;
        }

        problem.Extensions[ErrorCodeKey] = rejection.ErrorCode;
        problem.Extensions[CodeKey] = rejection.ErrorCode;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail, string? code = null)
    {
        var problem = StartProblem(context, status, title, detail);

        if (code != null)
        {
            problem.Extensions[ErrorCodeKey] = code;
            problem.Extensions[CodeKey] = code;
        }

        await context.Response.WriteAsJsonAsync(problem);
    }

    /// <summary>
    /// Sets status and problem+json content type on the response and returns the problem body for the caller to extend
    /// and write. WriteAsJsonAsync later replaces the content type with its own JSON type.
    /// </summary>
    /// <param name="status">HTTP status for both the response and the body</param>
    /// <param name="title">Short problem title</param>
    /// <param name="detail">Message shown to the client</param>
    private static ProblemDetails StartProblem(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = ProblemJsonContentType;

        return new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = detail
        };
    }
}
