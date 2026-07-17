// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolved on-call configuration: whether presence/standby on-call work changes count toward
/// worked hours at all, the per-category weighting factors (fractions 0..1, presence default 1.0 /
/// standby default 0.0) and whether the weighted hours count toward the K5 period caps. The single
/// type-to-factor mapping lives in <see cref="FactorFor"/> and the single settings-to-config parsing in
/// <see cref="FromSettings"/> so every hours path (OnCallConfigResolver, PeriodHoursService, the
/// WorkRepository cache-miss fallback) weights identically and cannot drift.
/// </summary>
/// <param name="Enabled">False disables the whole feature: on-call work changes contribute zero hours.</param>
/// <param name="PresenceFactor">Fraction (0..1) of raw hours an OnCallPresence change contributes.</param>
/// <param name="StandbyFactor">Fraction (0..1) of raw hours an OnCallStandby change contributes.</param>
/// <param name="IncludeInPeriodCaps">True lets the weighted hours count toward the K5 period caps.</param>
using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public sealed record OnCallConfig(
    bool Enabled,
    decimal PresenceFactor,
    decimal StandbyFactor,
    bool IncludeInPeriodCaps)
{
    private const decimal PercentDivisor = 100m;
    private const decimal DefaultPresencePercent = 100m;
    private const decimal DefaultStandbyPercent = 0m;

    public static readonly IReadOnlyList<string> RelevantSettingKeys = new[]
    {
        SettingKeys.WorktimeOnCallEnabled,
        SettingKeys.WorktimeOnCallPresenceCountsPercent,
        SettingKeys.WorktimeOnCallStandbyCountsPercent,
        SettingKeys.WorktimeOnCallIncludeInPeriodCaps,
    };

    /// <summary>
    /// Builds the config from the raw WORKTIME_ONCALL_* setting values (missing keys fall back to the
    /// documented defaults: disabled, presence 100 %, standby 0 %, excluded from caps). Percentages are
    /// clamped to 0..100 as a runtime safety net for a directly edited setting bypassing import validation.
    /// </summary>
    /// <param name="settings">Setting value lookup keyed by setting type; missing keys are treated as absent.</param>
    public static OnCallConfig FromSettings(IReadOnlyDictionary<string, string> settings)
    {
        var enabled = ParseBool(settings.GetValueOrDefault(SettingKeys.WorktimeOnCallEnabled));
        var presencePercent = ParsePercent(settings.GetValueOrDefault(SettingKeys.WorktimeOnCallPresenceCountsPercent), DefaultPresencePercent);
        var standbyPercent = ParsePercent(settings.GetValueOrDefault(SettingKeys.WorktimeOnCallStandbyCountsPercent), DefaultStandbyPercent);
        var includeInPeriodCaps = ParseBool(settings.GetValueOrDefault(SettingKeys.WorktimeOnCallIncludeInPeriodCaps));

        return new OnCallConfig(
            enabled,
            presencePercent / PercentDivisor,
            standbyPercent / PercentDivisor,
            includeInPeriodCaps);
    }

    /// <summary>
    /// Weighting factor (fraction 0..1) an on-call work change of the given type contributes to worked
    /// hours. Zero for non-on-call types and when the feature is disabled, so callers may multiply the
    /// raw ChangeTime unconditionally.
    /// </summary>
    /// <param name="type">The work change type being weighted.</param>
    public decimal FactorFor(WorkChangeType type)
    {
        if (!Enabled)
        {
            return 0m;
        }

        return type switch
        {
            WorkChangeType.OnCallPresence => PresenceFactor,
            WorkChangeType.OnCallStandby => StandbyFactor,
            _ => 0m,
        };
    }

    private static bool ParseBool(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static decimal ParsePercent(string? value, decimal fallback)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return fallback;
        }

        return Math.Clamp(parsed, 0m, PercentDivisor);
    }
}
