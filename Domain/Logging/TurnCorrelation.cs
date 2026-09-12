// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Ambient short id of the chat turn currently being processed, so log lines written by different
/// layers of the same turn can be joined. Deliberately an AsyncLocal and not a scoped DI service:
/// parts of a turn run inside child DI scopes of their own (RecipeEngineService creates one per
/// resolve), where a scoped service would hand out a fresh instance and produce a second, unrelated
/// id for the same turn. The value flows into child scopes and background continuations of the same
/// async flow automatically and is invisible to concurrent requests.
/// </summary>

namespace Klacks.Api.Domain.Logging;

public static class TurnCorrelation
{
    /// <summary>
    /// Placeholder logged when work runs outside a chat turn (background services, warmup).
    /// </summary>
    public const string None = "-";

    private const int ShortIdLength = 8;

    private static readonly AsyncLocal<string?> CurrentId = new();

    public static string? Current => CurrentId.Value;

    public static string CurrentOrNone => CurrentId.Value ?? None;

    /// <param name="turnId">Turn id every consumer of this turn already shares (LLMContext.TurnId)</param>
    public static void Set(Guid turnId) => CurrentId.Value = Format(turnId);

    /// <param name="turnId">Turn id to shorten for log output</param>
    public static string Format(Guid turnId) => turnId.ToString("N")[..ShortIdLength];
}
