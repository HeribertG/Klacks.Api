// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Country-agnostic delimited payroll export ("Tier A" in the country-pack research): a four-column
/// CSV (PersonnelNumber, Date, WageType, Quantity) with a header row, driven entirely by the group's
/// delimiter/encoding/wage-type mapping. Intended for target systems whose own import wizard lets the
/// customer map columns (Silae FR, Epsilon GR, Kulcs-Ber HU, and the shared Golf payroll vendors
/// ZenHR/Menaitech/Bayzat across SA/AE/QA/KW/BH/OM) rather than enforcing a fixed byte layout — one
/// group config selects this formatter for all of them, no per-country formatter class needed.
/// </summary>
/// <remarks>
/// Dates are emitted as ISO 8601 (yyyy-MM-dd) rather than a country-specific format, since this
/// formatter serves many locales at once and ISO avoids day/month ambiguity. Encoding defaults to
/// UTF-8 (the common case for these targets) but honours any encoding name configured on the group
/// (CodePagesEncodingProvider is registered globally, so both IANA names and Windows code pages work).
/// </remarks>
using System.Globalization;
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class GenericDelimitedPayrollExportFormatter : IPayrollExportFormatter
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string QuantityFormat = "F2";
    private const string DefaultEncodingName = "utf-8";
    private const string HeaderPersonnelNumber = "PersonnelNumber";
    private const string HeaderDate = "Date";
    private const string HeaderWageType = "WageType";
    private const string HeaderQuantity = "Quantity";

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyGenericPayrollCsv;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var delimiter = string.IsNullOrEmpty(config.Delimiter)
            ? PayrollExportConstants.DefaultDelimiter
            : config.Delimiter;
        var absenceMapping = ParseAbsenceMapping(config.AbsenceMappingJson);

        var sb = new StringBuilder();
        var recordCount = 0;
        var skippedAbsenceCount = 0;

        AppendRow(sb, delimiter, HeaderPersonnelNumber, HeaderDate, HeaderWageType, HeaderQuantity);

        foreach (var employee in data.Employees)
        {
            var personnelNumber = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var entry in employee.Entries)
            {
                var date = entry.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
                var quantity = entry.Quantity.ToString(QuantityFormat, CultureInfo.InvariantCulture);

                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours:
                        AppendRow(sb, delimiter, personnelNumber, date, config.BaseWageType, quantity);
                        recordCount++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (string.IsNullOrEmpty(config.SurchargeWageType))
                        {
                            continue;
                        }

                        AppendRow(sb, delimiter, personnelNumber, date, config.SurchargeWageType, quantity);
                        recordCount++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                        {
                            skippedAbsenceCount++;
                            continue;
                        }

                        AppendRow(sb, delimiter, personnelNumber, date, mapping.WageType, quantity);
                        recordCount++;
                        break;
                }
            }
        }

        return new PayrollExportResult
        {
            Content = ResolveEncoding(config.Encoding).GetBytes(sb.ToString()),
            RecordCount = recordCount,
            SkippedAbsenceCount = skippedAbsenceCount,
        };
    }

    private static void AppendRow(StringBuilder sb, string delimiter, params string[] fields)
    {
        sb.Append(string.Join(delimiter, fields.Select(f => Sanitize(f, delimiter))));
        sb.Append(PayrollExportConstants.LineEnding);
    }

    private static string Sanitize(string? value, string delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Replace(delimiter, string.Empty);
    }

    private static Dictionary<string, PayrollAbsenceMapping> ParseAbsenceMapping(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, PayrollAbsenceMapping>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, PayrollAbsenceMapping>>(json, MappingJsonOptions)
                ?? new Dictionary<string, PayrollAbsenceMapping>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, PayrollAbsenceMapping>();
        }
    }

    private static Encoding ResolveEncoding(string? encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
        {
            return Encoding.GetEncoding(DefaultEncodingName);
        }

        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            return Encoding.GetEncoding(DefaultEncodingName);
        }
    }
}
