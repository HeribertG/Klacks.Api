// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the three configurable caps the escalation chain's time-budget arithmetic depends on
/// (docs/ENTWURF-eskalationskette-2026-08-16.md §5), with the Entwurf's documented fallbacks. These
/// are runtime Settings rows, not BackgroundServiceOptions: an operator can tighten or loosen them
/// without a restart, unlike the sweep cadence.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;
using SettingKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public static class EscalationTimeBudgetReader
{
    private const int FallbackMinStageMinutes = 5;
    private const int FallbackMaxStageMinutes = 30;
    private const int FallbackPrepBufferHours = 2;

    public static async Task<EscalationTimeBudget> ReadAsync(ISettingsReader settingsReader, CancellationToken cancellationToken)
    {
        var min = await ReadIntAsync(settingsReader, SettingKeys.ESCALATION_STAGE_MIN_MINUTES, FallbackMinStageMinutes, cancellationToken);
        var max = await ReadIntAsync(settingsReader, SettingKeys.ESCALATION_STAGE_MAX_MINUTES, FallbackMaxStageMinutes, cancellationToken);
        var prepBuffer = await ReadIntAsync(settingsReader, SettingKeys.ESCALATION_PREP_BUFFER_HOURS, FallbackPrepBufferHours, cancellationToken);

        return new EscalationTimeBudget(min, Math.Max(max, min), prepBuffer);
    }

    private static async Task<int> ReadIntAsync(ISettingsReader settingsReader, string type, int fallback, CancellationToken cancellationToken)
    {
        var setting = await settingsReader.GetSetting(type);
        var raw = setting?.Value;

        if (!string.IsNullOrWhiteSpace(raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0)
        {
            return parsed;
        }

        return fallback;
    }
}
