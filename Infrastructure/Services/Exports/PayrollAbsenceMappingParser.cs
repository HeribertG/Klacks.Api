// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parses a group's AbsenceMappingJson (absence id to the target system's mapping payload) for a payroll
/// formatter. An empty value is a valid empty mapping. A value that is not valid JSON is not silently treated as
/// "no mapping": the counter is flagged AbsenceMappingInvalid, so the export log and the download report it, and
/// every absence of the export is then counted as unmapped.
/// </summary>
/// <param name="json">The stored AbsenceMappingJson</param>
/// <param name="counter">Receives the invalid-mapping flag</param>

using System.Text.Json;

namespace Klacks.Api.Infrastructure.Services.Exports;

public static class PayrollAbsenceMappingParser
{
    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static Dictionary<string, TMapping> Parse<TMapping>(string? json, PayrollExportSkipCounter counter)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, TMapping>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, TMapping>>(json, MappingJsonOptions)
                ?? new Dictionary<string, TMapping>();
        }
        catch (JsonException)
        {
            counter.AbsenceMappingInvalid = true;
            return new Dictionary<string, TMapping>();
        }
    }
}