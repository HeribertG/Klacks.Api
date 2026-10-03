// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Strict parser of PlanningConstraint.ParametersJson. The JSON must be an object carrying a supported
/// schemaVersion (older versions are first upgraded by PlanningConstraintParametersUpgrader) and exactly the
/// properties of its kind: unknown properties, missing required ones, anything but an exact member name where
/// an enum is expected (case-insensitive; no comma lists, blanks or numbers) and out-of-range
/// numbers are all reported. Optional: proRata (default true) and weekendDays (required only for the
/// WeekendDays metric).
/// </summary>

using System.Text.Json;
using System.Text.Json.Nodes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Services.Schedules;

public static class PlanningConstraintParametersParser
{
    private static readonly string[] MaxConsecutiveProperties =
        [PlanningConstraintDefaults.SchemaVersionProperty, PlanningConstraintDefaults.KindProperty, PlanningConstraintDefaults.MaxRunProperty];

    private static readonly string[] ForbiddenTransitionProperties =
        [PlanningConstraintDefaults.SchemaVersionProperty, PlanningConstraintDefaults.FromProperty, PlanningConstraintDefaults.ToProperty, PlanningConstraintDefaults.WithinDaysProperty];

    private static readonly string[] RestAfterKindProperties =
        [PlanningConstraintDefaults.SchemaVersionProperty, PlanningConstraintDefaults.KindProperty, PlanningConstraintDefaults.FreeDaysProperty];

    private static readonly string[] TeamFairnessProperties =
    [
        PlanningConstraintDefaults.SchemaVersionProperty, PlanningConstraintDefaults.MetricProperty, PlanningConstraintDefaults.WindowProperty,
        PlanningConstraintDefaults.MaxSpreadProperty, PlanningConstraintDefaults.ProRataProperty, PlanningConstraintDefaults.WeekendDaysProperty,
    ];

    public static PlanningConstraintParameters? Parse(PlanningConstraintKind kind, string? json, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            errors.Add("ParametersJson is required.");
            return null;
        }

        try
        {
            if (JsonNode.Parse(json) is not JsonObject stored)
            {
                errors.Add("ParametersJson must be a JSON object.");
                return null;
            }

            if (!PlanningConstraintParametersUpgrader.TryUpgrade(stored, errors, out var current))
            {
                return null;
            }

            using var document = JsonDocument.Parse(current.ToJsonString());
            var root = document.RootElement;

            var errorCountBefore = errors.Count;
            PlanningConstraintParameters? parameters = kind switch
            {
                PlanningConstraintKind.MaxConsecutiveOfKind => ParseMaxConsecutive(root, errors),
                PlanningConstraintKind.ForbiddenTransition => ParseForbiddenTransition(root, errors),
                PlanningConstraintKind.RestAfterKind => ParseRestAfterKind(root, errors),
                PlanningConstraintKind.TeamFairness => ParseTeamFairness(root, errors),
                _ => null,
            };

            if (parameters is null && errors.Count == errorCountBefore)
            {
                errors.Add("Kind must be a defined planning constraint kind.");
            }

            return errors.Count == errorCountBefore ? parameters : null;
        }
        catch (JsonException)
        {
            errors.Add("ParametersJson is not valid JSON.");
            return null;
        }
    }

    private static MaxConsecutiveOfKindParameters? ParseMaxConsecutive(JsonElement root, List<string> errors)
    {
        CheckEnvelope(root, MaxConsecutiveProperties, errors);
        var shiftKind = ReadEnum<PlanningShiftKind>(root, PlanningConstraintDefaults.KindProperty, errors);
        var maxRun = ReadInt(root, PlanningConstraintDefaults.MaxRunProperty, 1, PlanningConstraintDefaults.MaxRunLimit, errors);
        return shiftKind.HasValue && maxRun.HasValue ? new MaxConsecutiveOfKindParameters(shiftKind.Value, maxRun.Value) : null;
    }

    private static ForbiddenTransitionParameters? ParseForbiddenTransition(JsonElement root, List<string> errors)
    {
        CheckEnvelope(root, ForbiddenTransitionProperties, errors);
        var from = ReadEnum<PlanningShiftKind>(root, PlanningConstraintDefaults.FromProperty, errors);
        var to = ReadEnum<PlanningShiftKind>(root, PlanningConstraintDefaults.ToProperty, errors);
        var withinDays = ReadInt(root, PlanningConstraintDefaults.WithinDaysProperty, 1, PlanningConstraintDefaults.MaxDayDistanceLimit, errors);
        return from.HasValue && to.HasValue && withinDays.HasValue
            ? new ForbiddenTransitionParameters(from.Value, to.Value, withinDays.Value)
            : null;
    }

    private static RestAfterKindParameters? ParseRestAfterKind(JsonElement root, List<string> errors)
    {
        CheckEnvelope(root, RestAfterKindProperties, errors);
        var shiftKind = ReadEnum<PlanningShiftKind>(root, PlanningConstraintDefaults.KindProperty, errors);
        var freeDays = ReadInt(root, PlanningConstraintDefaults.FreeDaysProperty, 1, PlanningConstraintDefaults.MaxDayDistanceLimit, errors);
        return shiftKind.HasValue && freeDays.HasValue ? new RestAfterKindParameters(shiftKind.Value, freeDays.Value) : null;
    }

    private static TeamFairnessParameters? ParseTeamFairness(JsonElement root, List<string> errors)
    {
        CheckEnvelope(root, TeamFairnessProperties, errors);
        var metric = ReadEnum<PlanningFairnessMetric>(root, PlanningConstraintDefaults.MetricProperty, errors);
        var window = ReadEnum<PlanningFairnessWindow>(root, PlanningConstraintDefaults.WindowProperty, errors);
        var maxSpread = ReadNonNegativeDecimal(root, PlanningConstraintDefaults.MaxSpreadProperty, errors);
        var proRata = ReadOptionalBool(root, PlanningConstraintDefaults.ProRataProperty, PlanningConstraintDefaults.DefaultProRata, errors);
        var weekendDays = ReadWeekendDays(root, errors);

        if (metric == PlanningFairnessMetric.WeekendDays && weekendDays is { Count: 0 })
        {
            errors.Add($"'{PlanningConstraintDefaults.WeekendDaysProperty}' is required and must not be empty for the WeekendDays metric.");
            return null;
        }

        return metric.HasValue && window.HasValue && maxSpread.HasValue && proRata.HasValue && weekendDays is not null
            ? new TeamFairnessParameters(metric.Value, window.Value, maxSpread.Value, proRata.Value, weekendDays)
            : null;
    }

    private static void CheckEnvelope(JsonElement root, string[] allowedProperties, List<string> errors)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (!allowedProperties.Contains(property.Name, StringComparer.Ordinal))
            {
                errors.Add($"Unknown parameter '{property.Name}'.");
            }
        }
    }

    private static TEnum? ReadEnum<TEnum>(JsonElement root, string name, List<string> errors)
        where TEnum : struct, Enum
    {
        if (!root.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String)
        {
            errors.Add($"'{name}' is required and must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return null;
        }

        if (TryParseExactName<TEnum>(element.GetString(), out var parsed))
        {
            return parsed;
        }

        errors.Add($"'{name}' must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        return null;
    }

    /// <summary>
    /// Exact, case-insensitive match against the declared member names. Enum.TryParse is deliberately not used:
    /// it accepts comma lists ("Work,Early" = the OR of both values), surrounding blanks and numbers.
    /// </summary>
    private static bool TryParseExactName<TEnum>(string? text, out TEnum value)
        where TEnum : struct, Enum
    {
        foreach (var name in Enum.GetNames<TEnum>())
        {
            if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
            {
                value = Enum.Parse<TEnum>(name);
                return true;
            }
        }

        value = default;
        return false;
    }

    private static int? ReadInt(JsonElement root, string name, int min, int max, List<string> errors)
    {
        if (root.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out var value)
            && value >= min
            && value <= max)
        {
            return value;
        }

        errors.Add($"'{name}' is required and must be a whole number between {min} and {max}.");
        return null;
    }

    private static decimal? ReadNonNegativeDecimal(JsonElement root, string name, List<string> errors)
    {
        if (root.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetDecimal(out var value)
            && value >= 0m)
        {
            return value;
        }

        errors.Add($"'{name}' is required and must be a number not below 0.");
        return null;
    }

    private static bool? ReadOptionalBool(JsonElement root, string name, bool defaultValue, List<string> errors)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return defaultValue;
        }

        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        errors.Add($"'{name}' must be true or false.");
        return null;
    }

    private static IReadOnlySet<DayOfWeek>? ReadWeekendDays(JsonElement root, List<string> errors)
    {
        var days = new HashSet<DayOfWeek>();
        if (!root.TryGetProperty(PlanningConstraintDefaults.WeekendDaysProperty, out var element))
        {
            return days;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"'{PlanningConstraintDefaults.WeekendDaysProperty}' must be an array of day names.");
            return null;
        }

        foreach (var item in element.EnumerateArray())
        {
            var text = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (!TryParseExactName<DayOfWeek>(text, out var day))
            {
                errors.Add($"'{PlanningConstraintDefaults.WeekendDaysProperty}' must contain day names (Monday..Sunday).");
                return null;
            }

            days.Add(day);
        }

        return days;
    }
}
