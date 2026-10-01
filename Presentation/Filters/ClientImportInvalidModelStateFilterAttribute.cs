// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Controller-scoped replacement for ASP.NET's automatic invalid-model-state 400 on the employee import:
/// a body the framework cannot bind (for example an unknown enum name) is rejected as invalid-request with
/// the import's code, so the UI can translate it. It runs before the framework's ModelStateInvalidFilter
/// and leaves every other controller untouched. Only the offending field paths reach the client, never
/// the JSON library's message.
/// </summary>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ClientImportInvalidModelStateFilterAttribute : ActionFilterAttribute
{
    public const int RunsBeforeModelStateInvalidFilter = -3000;

    private const string FieldSeparator = ", ";

    public ClientImportInvalidModelStateFilterAttribute()
    {
        Order = RunsBeforeModelStateInvalidFilter;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var fields = string.Join(FieldSeparator, context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => entry.Key));

        throw new ClientImportRejectedException(
            ClientImportErrorCodes.InvalidRequest,
            $"The request could not be read. Invalid fields: {fields}");
    }
}
