// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A macro read without tracking, so that a macro switch can never modify or re-categorise it.
/// </summary>
/// <param name="Id">Id of the macro</param>
/// <param name="Name">Name of the macro</param>
/// <param name="Type">MacroFunctionEnum value (Custom, Standard, StandardAdditive)</param>
/// <param name="Category">What the macro is categorised for (shift, an absence kind, or unspecified)</param>
/// <param name="Origin">Who created the macro</param>
/// <param name="Content">The script</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Macros;

public record MacroSnapshot(
    Guid Id,
    string Name,
    int Type,
    MacroCategoryEnum Category,
    MacroOrigin Origin,
    string Content);
