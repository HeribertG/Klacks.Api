// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats the day-based entries of a payroll period (PayrollQuantityUnit.Days, i.e. on-call duty days) as a
/// BrightPay "Import Daily Payments" CSV for Ireland and the United Kingdom. Header row: Works number, Description,
/// Number of normal days. It complements the hourly BrightpayIeUkExportFormatter, whose layout has no day column.
/// </summary>
/// <remarks>
/// Field names verified 2026-10-07 against
/// https://www.brightpay.co.uk/docs/24-25/importing-pay-data-using-csv-file/importing-daily-payments-using-csv-file/
/// (employee match fields first name / surname / NINO / works number; per payment "Rate per day (if different from
/// standard rate)", "Description", "Number of normal days" and further multiplier tiers). Like the hourly format,
/// the employee is matched by "Works number" (employee.IdNumber) and "Rate per day" is omitted, so BrightPay uses
/// the employee's standard daily rate; BrightPay maps columns by header name. Each mapped day-based absence becomes
/// one row with PayrollAbsenceMapping.WageType as Description. Hour-based entries (worked hours, surcharges,
/// ordinary absences) have no place in a daily import and are counted in SkippedUnsupportedUnitCount; they belong in
/// the hourly file. Unmapped absences are counted in SkippedAbsenceCount.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class BrightpayIeUkDailyExportFormatter : IPayrollExportFormatter
{
    private const string QuantityFormat = "F2";
    private const string DefaultDelimiterComma = ",";
    private const string DefaultEncodingName = "utf-8";
    private const string HeaderWorksNumber = "Works number";
    private const string HeaderDescription = "Description";
    private const string HeaderNormalDays = "Number of normal days";
    private const string EmptyField = "";

    public string FormatKey => PayrollExportConstants.FormatKeyBrightpayIeUkDaily;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var delimiter = string.IsNullOrEmpty(config.Delimiter)
            ? DefaultDelimiterComma
            : config.Delimiter;
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PayrollAbsenceMapping>(config.AbsenceMappingJson, counter);

        var sb = new StringBuilder();
        AppendRow(sb, delimiter, HeaderWorksNumber, HeaderDescription, HeaderNormalDays);

        foreach (var employee in data.Employees)
        {
            var worksNumber = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var entry in employee.Entries)
            {
                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours or PayrollEntryKind.Surcharge:
                        counter.UnsupportedUnits++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                        {
                            counter.UnmappedAbsences++;
                            break;
                        }

                        if (entry.Unit != PayrollQuantityUnit.Days)
                        {
                            counter.UnsupportedUnits++;
                            break;
                        }

                        AppendRow(
                            sb,
                            delimiter,
                            worksNumber,
                            CsvFormulaGuard.Neutralize(mapping.WageType),
                            entry.Quantity.ToString(QuantityFormat, CultureInfo.InvariantCulture));
                        counter.Emitted++;
                        break;

                    default:
                        counter.UnsupportedKinds++;
                        break;
                }
            }
        }

        return counter.ToResult(ResolveEncoding(config.Encoding).GetBytes(sb.ToString()), counter.Emitted);
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