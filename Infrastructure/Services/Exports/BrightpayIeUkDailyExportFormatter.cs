// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats the day-based entries of a payroll period (PayrollQuantityUnit.Days, i.e. on-call duty days) as a
/// BrightPay "Import Daily Payments" CSV for Ireland and the United Kingdom: one row per employee, comma-separated,
/// header row first. Header row: "Employee works number", then five payment blocks "Payment n - Description",
/// "Payment n - Number of normal days" (n = 1..5). It complements the hourly BrightpayIeUkExportFormatter, whose
/// import has no day column.
/// </summary>
/// <remarks>
/// Field set verified 2026-11-02 against
/// UK https://www.brightpay.co.uk/docs/24-25/importing-pay-data-using-csv-file/importing-daily-payments-using-csv-file/
/// (template linked there: https://downloads.thesaurussoftware.com/csv/DailyPaymentsCSV.csv, not retrievable here) and
/// IE https://www.brightpay.ie/docs/2026/importing-pay-data-from-a-csv-file/importing-daily-payments-using-csv-file/:
/// match fields Employee first name / Employee surname / Employee NINO (UK) or Employee PPSN (IE) / Employee works
/// number; per row "Payment 1" to "Payment 5" blocks of "Rate per day (if different from standard rate)",
/// "Description", "Number of normal days", the multiplier tiers, "Department" and deductibility flags. Layout rules
/// are shared with the hourly formatter (see its remarks): "Payment n - field" header labels for "Match Header Row",
/// works number as the only identifier, one row per employee with per-description period totals (the wizard's
/// "Replace Existing" makes several same-description rows unsafe), continuation row after five descriptions, comma
/// delimiter fixed (config.Delimiter ignored), "." decimals with two places, "Rate per day" and "Department" omitted
/// so BrightPay uses the employee's standard daily rate. Each mapped day-based absence is totalled under
/// PayrollAbsenceMapping.WageType. Hour-based entries (worked hours, surcharges, ordinary absences) have no place in a
/// daily import and are counted in SkippedUnsupportedUnitCount; they belong in the hourly file. Unmapped absences
/// and mapped absences with a blank WageType are counted in SkippedAbsenceCount. RecordCount is the number of data rows.
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
    private const int PaymentBlocksPerRow = 5;
    private const string QuantityFormat = "F2";
    private const string Delimiter = ",";
    private const string DefaultEncodingName = "utf-8";
    private const string HeaderWorksNumber = "Employee works number";
    private const string PaymentHeaderFormat = "Payment {0} - {1}";
    private const string FieldDescription = "Description";
    private const string FieldNormalDays = "Number of normal days";
    private const string EmptyField = "";

    private static readonly string[] HeaderFields = BuildHeader();

    public string FormatKey => PayrollExportConstants.FormatKeyBrightpayIeUkDaily;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PayrollAbsenceMapping>(config.AbsenceMappingJson, counter);

        var sb = new StringBuilder();
        AppendRow(sb, HeaderFields);
        var recordCount = 0;

        foreach (var employee in data.Employees)
        {
            var descriptions = new List<string>();
            var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);

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

                        if (!TryAddToTotal(descriptions, totals, mapping.WageType, entry.Quantity))
                        {
                            counter.UnmappedAbsences++;
                            break;
                        }

                        counter.Emitted++;
                        break;

                    default:
                        counter.UnsupportedKinds++;
                        break;
                }
            }

            recordCount += AppendEmployeeRows(sb, employee.IdNumber.ToString(CultureInfo.InvariantCulture), descriptions, totals);
        }

        return counter.ToResult(ResolveEncoding(config.Encoding).GetBytes(sb.ToString()), recordCount);
    }

    private static string[] BuildHeader()
    {
        var fields = new List<string> { HeaderWorksNumber };
        for (var block = 1; block <= PaymentBlocksPerRow; block++)
        {
            fields.Add(string.Format(CultureInfo.InvariantCulture, PaymentHeaderFormat, block, FieldDescription));
            fields.Add(string.Format(CultureInfo.InvariantCulture, PaymentHeaderFormat, block, FieldNormalDays));
        }

        return fields.ToArray();
    }

    private static bool TryAddToTotal(List<string> descriptions, Dictionary<string, decimal> totals, string? wageType, decimal quantity)
    {
        var description = CsvFormulaGuard.Neutralize(Sanitize(wageType).Trim());
        if (description.Length == 0)
        {
            return false;
        }

        if (totals.TryGetValue(description, out var total))
        {
            totals[description] = total + quantity;
            return true;
        }

        descriptions.Add(description);
        totals[description] = quantity;
        return true;
    }

    private static int AppendEmployeeRows(StringBuilder sb, string worksNumber, List<string> descriptions, Dictionary<string, decimal> totals)
    {
        var rows = 0;
        for (var start = 0; start < descriptions.Count; start += PaymentBlocksPerRow)
        {
            var fields = new List<string> { worksNumber };
            for (var offset = 0; offset < PaymentBlocksPerRow; offset++)
            {
                var index = start + offset;
                if (index < descriptions.Count)
                {
                    fields.Add(descriptions[index]);
                    fields.Add(totals[descriptions[index]].ToString(QuantityFormat, CultureInfo.InvariantCulture));
                }
                else
                {
                    fields.Add(EmptyField);
                    fields.Add(EmptyField);
                }
            }

            AppendRow(sb, fields);
            rows++;
        }

        return rows;
    }

    private static void AppendRow(StringBuilder sb, IEnumerable<string> fields)
    {
        sb.Append(string.Join(Delimiter, fields.Select(Sanitize)));
        sb.Append(PayrollExportConstants.LineEnding);
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return EmptyField;
        }

        return value
            .Replace("\r", EmptyField)
            .Replace("\n", EmptyField)
            .Replace("\"", EmptyField)
            .Replace(Delimiter, EmptyField);
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
