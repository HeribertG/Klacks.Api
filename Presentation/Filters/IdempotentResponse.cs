// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Successful answer of a creating action, kept so a repeat with the same Idempotency-Key gets the same answer.
/// </summary>
/// <param name="StatusCode">2xx status code of the first execution</param>
/// <param name="Value">Body object the first execution returned (carries the server-assigned id)</param>
/// <param name="DeclaredType">Declared body type of the first answer, used for output formatting</param>

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Filters;

public sealed record IdempotentResponse(int StatusCode, object Value, Type? DeclaredType)
{
    public ObjectResult ToResult() => new(Value) { StatusCode = StatusCode, DeclaredType = DeclaredType };
}
