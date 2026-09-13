// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reserved keys the executor adds to a skill's invocation arguments to carry execution context that
/// every skill may need but no skill declares. They use a double-underscore prefix so they cannot
/// collide with a declared parameter name, are added to a copy of the argument dictionary handed to
/// the skill only - never to the dictionary used for validation, usage tracking, UI actions, plugin
/// skills or the generic dispatcher - and are filtered out again by the usage-log redactor.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SkillParameterKeys
{
    public const string ReservedPrefix = "__";

    public const string UserLanguage = "__userLanguage";

    public static bool IsReserved(string? name) =>
        name is not null && name.StartsWith(ReservedPrefix, StringComparison.Ordinal);
}
