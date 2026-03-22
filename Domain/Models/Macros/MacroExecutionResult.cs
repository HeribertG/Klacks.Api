// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// Result of a macro execution with optional surcharges/work time value.
/// </summary>
/// <param name="Success">Indicates whether the macro execution was successful</param>
/// <param name="ResultValue">The extracted decimal value from the DefaultResult, or null if none exists</param>
public record MacroExecutionResult(bool Success, decimal? ResultValue);
