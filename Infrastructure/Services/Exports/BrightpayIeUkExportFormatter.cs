// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Formats employee-centric payroll data as a BrightPay "Import Hourly Payments" CSV. The file is
/// identical for Ireland and the United Kingdom — BrightPay's own import wizard lets the customer map
/// each CSV column to a payment field once ("Match Header Row" / "Remember Columns"), so column order
/// is not fixed; only the header names below must match what the customer selects in that wizard.
/// Header row: Works number, Description, Number of normal hours, Number of time and a half hours.
/// </summary>
/// <remarks>
/// BrightPay's confirmed field set (verified 2026-07-09 against
/// https://www.brightpay.co.uk/docs/24-25/importing-pay-data-using-csv-file/importing-hourly-payments-using-csv-file/
/// and https://www.brightpay.ie/docs/2020/importing-pay-data-from-a-csv-file/importing-hourly-payments-using-csv-file/)
/// offers, per payment line, a "Rate per hour", "Description", "Department" and separate hour columns
/// per multiplier tier (normal, time-and-a-half, time-and-a-third, time-and-a-quarter, double, triple,
/// quadruple), plus employee match fields (first name, surname, works number, and NINO for UK / PPSN for
/// IE). Employee matching here uses "Works number" only (employee.IdNumber) — Klacks does not hold
/// PPSN/NINO, and works number is sufficient to identify the employee in BrightPay, so one file layout
/// serves both countries. Klacks' PayrollEntryKind.Surcharge is a single generic kind with no multiplier
/// value of its own, so it is mapped to BrightPay's "time and a half" tier column as the single supported
/// tier; a different premium multiplier configured on the BrightPay side is a known limitation. Absence
/// rows reuse PayrollAbsenceMapping.WageType as the free-text Description (BrightPay has no absence-code
/// concept in this import) and are written into the "Number of normal hours" column; unmapped absences
/// are counted in SkippedAbsenceCount rather than emitted. "Rate per hour" and "Department" are omitted
/// entirely (not populated by Klacks data) rather than emitted empty, since BrightPay's wizard only maps
/// columns that are present. Unlike DatevLugBewegungsdatenFormatter (fixed 11-field layout, no header),
/// this formatter always writes a header row because BrightPay resolves columns by header name.
/// </remarks>
using System.Globalization;
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class BrightpayIeUkExportFormatter : IPayrollExportFormatter
{
    private const string QuantityFormat = "F2";
    private const string DefaultDelimiterComma = ",";
    private const string DefaultEncodingName = "utf-8";
    private const string HeaderWorksNumber = "Works number";
    private const string HeaderDescription = "Description";
    private const string HeaderNormalHours = "Number of normal hours";
    private const string HeaderTimeAndAHalfHours = "Number of time and a half hours";
    private const string EmptyField = "";

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyBrightpayIeUk;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var delimiter = string.IsNullOrEmpty(config.Delimiter)
            ? DefaultDelimiterComma
            : config.Delimiter;
        var absenceMapping = ParseAbsenceMapping(config.AbsenceMappingJson);

        var sb = new StringBuilder();
        var recordCount = 0;
        var skippedAbsenceCount = 0;

        AppendRow(sb, delimiter, HeaderWorksNumber, HeaderDescription, HeaderNormalHours, HeaderTimeAndAHalfHours);

        foreach (var employee in data.Employees)
        {
            var worksNumber = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var entry in employee.Entries)
            {
                var quantity = entry.Quantity.ToString(QuantityFormat, CultureInfo.InvariantCulture);

                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours:
                        AppendRow(sb, delimiter, worksNumber, config.BaseWageType, quantity, EmptyField);
                        recordCount++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (string.IsNullOrEmpty(config.SurchargeWageType))
                        {
                            continue;
                        }

                        AppendRow(sb, delimiter, worksNumber, config.SurchargeWageType, EmptyField, quantity);
                        recordCount++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                        {
                            skippedAbsenceCount++;
                            continue;
                        }

                        AppendRow(sb, delimiter, worksNumber, mapping.WageType, quantity, EmptyField);
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
            return EmptyField;
        }

        return value
            .Replace("\r", EmptyField)
            .Replace("\n", EmptyField)
            .Replace(delimiter, EmptyField);
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
