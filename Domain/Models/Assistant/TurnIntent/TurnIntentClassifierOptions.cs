// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runtime configuration of the turn-intent classifier, bound from Assistant:TurnIntentClassifier at startup.
/// FromConfiguration normalises raw values: an unknown mode switches the classifier off instead of silently running
/// it in an unintended mode, and the timeout is clamped to a sane range.
/// </summary>
/// <param name="Mode">Off, Shadow (classify and log only) or Active (classify, log and guarantee explain skills)</param>
/// <param name="TimeoutMilliseconds">Upper bound for waiting on the extra embedding call</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant.TurnIntent;

public sealed record TurnIntentClassifierOptions(string Mode, int TimeoutMilliseconds)
{
    private static readonly string[] KnownModes =
        [TurnIntentClassifierModes.Off, TurnIntentClassifierModes.Shadow, TurnIntentClassifierModes.Active];

    public static TurnIntentClassifierOptions Off { get; } =
        new(TurnIntentClassifierModes.Off, TurnIntentDefaults.DefaultTimeoutMilliseconds);

    public bool IsEnabled => !string.Equals(Mode, TurnIntentClassifierModes.Off, StringComparison.OrdinalIgnoreCase);

    public bool IsActive => string.Equals(Mode, TurnIntentClassifierModes.Active, StringComparison.OrdinalIgnoreCase);

    public static TurnIntentClassifierOptions FromConfiguration(string? mode, int timeoutMilliseconds)
    {
        var knownMode = KnownModes.FirstOrDefault(known => string.Equals(known, mode?.Trim(), StringComparison.OrdinalIgnoreCase))
                        ?? TurnIntentClassifierModes.Off;
        var timeout = Math.Clamp(
            timeoutMilliseconds, TurnIntentDefaults.MinimumTimeoutMilliseconds, TurnIntentDefaults.MaximumTimeoutMilliseconds);
        return new TurnIntentClassifierOptions(knownMode, timeout);
    }
}
