// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parsed export format override patch: whitelisted scalar values plus an optional absence-mapping
/// object, applied over the runtime parameter objects (ExportOptions / PayrollExportGroupConfig).
/// Parse accepts only string values (absenceMapping: object); Validate reports unknown keys for a
/// format family so a support patch cannot silently do nothing.
/// </summary>
/// <param name="scalars">Whitelisted key/value pairs from the patch JSON</param>
/// <param name="absenceMappingJson">Raw JSON object replacing the config's absence mapping, when present</param>
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Application.Services.Exports;

public sealed class ExportFormatOverridePatch
{
    private readonly IReadOnlyDictionary<string, string> _scalars;
    private readonly string? _absenceMappingJson;

    private ExportFormatOverridePatch(IReadOnlyDictionary<string, string> scalars, string? absenceMappingJson)
    {
        _scalars = scalars;
        _absenceMappingJson = absenceMappingJson;
    }

    public bool IsEmpty => _scalars.Count == 0 && _absenceMappingJson is null;

    public static ExportFormatOverridePatch Parse(string patchJson)
    {
        using var document = JsonDocument.Parse(patchJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Override patch must be a JSON object.");
        }

        var scalars = new Dictionary<string, string>(StringComparer.Ordinal);
        string? absenceMappingJson = null;

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Name == ExportOverrideConstants.KeyAbsenceMapping)
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new FormatException($"'{ExportOverrideConstants.KeyAbsenceMapping}' must be a JSON object.");
                }

                absenceMappingJson = property.Value.GetRawText();
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                throw new FormatException($"Override value for '{property.Name}' must be a string.");
            }

            scalars[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return new ExportFormatOverridePatch(scalars, absenceMappingJson);
    }

    public static IReadOnlyList<string> Validate(string patchJson, string family)
    {
        var errors = new List<string>();
        ExportFormatOverridePatch patch;
        try
        {
            patch = Parse(patchJson);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return [ex.Message];
        }

        if (patch.IsEmpty)
        {
            errors.Add("Override patch contains no values.");
        }

        var allowed = ExportOverrideConstants.KeysForFamily(family);
        foreach (var key in patch._scalars.Keys)
        {
            if (!allowed.Contains(key))
            {
                errors.Add($"Unknown override key for family '{family}': '{key}'. Allowed: {string.Join(", ", allowed)}.");
            }
        }

        if (patch._absenceMappingJson is not null && !allowed.Contains(ExportOverrideConstants.KeyAbsenceMapping))
        {
            errors.Add($"'{ExportOverrideConstants.KeyAbsenceMapping}' is only allowed for family '{ExportConstants.FormatFamilyPayroll}'.");
        }

        return errors;
    }

    public void ApplyTo(ExportOptions options)
    {
        options.DateFormat = Get(ExportOverrideConstants.KeyDateFormat) ?? options.DateFormat;
        options.TimeFormat = Get(ExportOverrideConstants.KeyTimeFormat) ?? options.TimeFormat;
        options.CurrencyCode = Get(ExportOverrideConstants.KeyCurrencyCode) ?? options.CurrencyCode;
        options.Language = Get(ExportOverrideConstants.KeyLanguage) ?? options.Language;
    }

    public void ApplyTo(PayrollExportGroupConfig config)
    {
        config.Delimiter = Get(ExportOverrideConstants.KeyDelimiter) ?? config.Delimiter;
        config.Encoding = Get(ExportOverrideConstants.KeyEncoding) ?? config.Encoding;
        config.BaseWageType = Get(ExportOverrideConstants.KeyBaseWageType) ?? config.BaseWageType;
        config.SurchargeWageType = Get(ExportOverrideConstants.KeySurchargeWageType) ?? config.SurchargeWageType;
        config.AbsenceMappingJson = _absenceMappingJson ?? config.AbsenceMappingJson;
    }

    private string? Get(string key) => _scalars.TryGetValue(key, out var value) ? value : null;
}
