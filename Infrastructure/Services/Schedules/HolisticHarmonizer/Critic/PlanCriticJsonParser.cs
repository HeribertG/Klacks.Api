// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Parses the critic answer ({"findings":[...]}) leniently: surrounding prose or code fences are ignored, a finding
/// with a missing field or an unknown severity is still read (severity falls back to Medium), and a cell without
/// integer row and day is dropped. Whether a finding is TRUE is not decided here - the verifier compares it with the
/// real plan afterwards.
/// </summary>
public static class PlanCriticJsonParser
{
    private const string FindingsProperty = "findings";
    private const string PatternProperty = "pattern";
    private const string SeverityProperty = "severity";
    private const string DescriptionProperty = "description";
    private const string CellsProperty = "cells";
    private const string RowProperty = "row";
    private const string DayProperty = "day";
    private const string SymbolProperty = "symbol";

    private const string NoJsonError = "Critic answer contained no JSON object.";
    private const string NoFindingsArrayError = "Critic answer has no \"findings\" array.";
    private const string InvalidJsonError = "Critic answer is not valid JSON: {0}";

    public static IReadOnlyList<PlanCriticFinding> TryParse(string raw, out string? error)
    {
        error = null;
        var json = HarmonyJsonParser.ExtractJsonObject(raw);
        if (json is null)
        {
            error = NoJsonError;
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(FindingsProperty, out var findings)
                || findings.ValueKind != JsonValueKind.Array)
            {
                error = NoFindingsArrayError;
                return [];
            }

            return findings.EnumerateArray()
                .Where(f => f.ValueKind == JsonValueKind.Object)
                .Select(ReadFinding)
                .ToList();
        }
        catch (JsonException ex)
        {
            error = string.Format(System.Globalization.CultureInfo.InvariantCulture, InvalidJsonError, ex.Message);
            return [];
        }
    }

    private static PlanCriticFinding ReadFinding(JsonElement element)
    {
        IReadOnlyList<PlanCriticCell> cells = element.TryGetProperty(CellsProperty, out var cellArray) && cellArray.ValueKind == JsonValueKind.Array
            ? cellArray.EnumerateArray().Select(ReadCell).OfType<PlanCriticCell>().ToList()
            : [];

        return new PlanCriticFinding(
            ReadString(element, PatternProperty),
            ReadString(element, DescriptionProperty),
            ParseSeverity(ReadString(element, SeverityProperty)),
            cells);
    }

    private static PlanCriticCell? ReadCell(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !TryReadInt(element, RowProperty, out var row)
            || !TryReadInt(element, DayProperty, out var day))
        {
            return null;
        }

        return new PlanCriticCell(row, day, ReadString(element, SymbolProperty));
    }

    private static bool TryReadInt(JsonElement parent, string name, out int value)
    {
        value = 0;
        return parent.TryGetProperty(name, out var element)
               && element.ValueKind == JsonValueKind.Number
               && element.TryGetInt32(out value);
    }

    private static string ReadString(JsonElement parent, string name)
        => parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : string.Empty;

    private static PlanCriticSeverity ParseSeverity(string value)
        => Enum.TryParse<PlanCriticSeverity>(value.Trim(), ignoreCase: true, out var severity)
           && Enum.IsDefined(severity)
            ? severity
            : PlanCriticSeverity.Medium;
}
