// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats employee-centric payroll data as a BrightPay "Import Hourly Payments" CSV for Ireland and the United
/// Kingdom: one row per employee, comma-separated, header row first. Header row: "Employee works number", then five
/// payment blocks "Payment n - Description", "Payment n - Number of normal hours" (n = 1..5). Each block carries the
/// period total of one Klacks wage type (base wage, surcharge wage, or a mapped absence's WageType) for that employee.
/// </summary>
/// <remarks>
/// Field set verified 2026-11-02 against BrightPay's own documentation:
/// UK https://www.brightpay.co.uk/docs/25-26/importing-pay-data-using-csv-file/importing-hourly-payments-using-csv-file/
/// (also /docs/24-25/...), IE https://www.brightpay.ie/docs/2026/importing-pay-data-from-a-csv-file/importing-hourly-payments-using-csv-file/
/// (also /docs/2024/...). Both list, "for matching to employee record", Employee first name / Employee surname /
/// Employee NINO (UK) or Employee PPSN (IE) / Employee works number, and per row "Payment 1" to "Payment 5" blocks of
/// "Rate per hour (if different from standard rate)", "Description", "Number of normal hours", the multiplier tiers
/// (time and a half ... quadruple), "Department" and country-specific deductibility flags (IE 2024+ also "Code").
/// The labels written here use the "Payment n - field" form shown by BrightPay's column picker (as quoted by the
/// TimeKeeper integration guide https://help-v2.timekeeper.co.uk/importing-into-brightpay-desktop-hg6k7h-66vnw,
/// "Payment 1 - Number of Normal hours") so that "Match Header Row" can pre-select them; the customer can map columns
/// by hand and "Remember Columns" in any case. Only the works number is used as identifier (Klacks holds neither
/// NINO nor PPSN), so one layout serves both countries. Decimals use "." (UK/IE convention), two places.
/// Deliberate decisions:
/// - One row per employee, not one row per day: the spec's layout repeats the match fields once per row with up to
///   five payments, and the wizard's "Replace Existing" option replaces an equivalent pay item already on the
///   payslip, so several rows with the same description for one employee would at best rely on undocumented summing
///   and at worst keep only the last day. Totals are therefore summed per employee and description over the period.
///   An employee with more than five distinct descriptions continues on a further row with the same works number
///   (distinct descriptions, so no replace clash); BrightPay's handling of such continuation rows is not documented.
/// - The delimiter is fixed to a comma (the file is a CSV for BrightPay's CSV import, and the vendor templates are
///   comma-separated); config.Delimiter is ignored because the installation default is ";". config.Encoding is
///   still honoured (the vendor names none; only ASCII digits and the configured descriptions are written).
/// - Surcharges are Klacks time credits that already contain the premium, so they are written as normal hours under
///   SurchargeWageType; a multiplier tier (e.g. time and a half) would pay the premium twice.
/// - "Rate per hour" and "Department" are not written, so BrightPay applies the employee's standard hourly rate.
/// Absence rows use PayrollAbsenceMapping.WageType as Description; unmapped absences, and mapped absences whose
/// WageType is blank, are counted in SkippedAbsenceCount. Worked hours without a BaseWageType are counted in
/// SkippedUnmappedBaseWageCount and surcharges without a SurchargeWageType in SkippedUnmappedSurchargeCount: a
/// payment without a description could not be told apart from (and would be replaced together with) any other
/// undescribed pay item. Day-based entries (PayrollQuantityUnit.Days, on-call duty days) cannot be expressed in hours
/// and are counted in SkippedUnsupportedUnitCount; they belong in BrightpayIeUkDailyExportFormatter's file.
/// RecordCount is the number of data rows (employees, plus continuation rows).
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class BrightpayIeUkExportFormatter : IPayrollExportFormatter
{
    private const int PaymentBlocksPerRow = 5;

    private const string QuantityFormat = "F2";
    private const string Delimiter = ",";
    private const string DefaultEncodingName = "utf-8";
    private const string HeaderWorksNumber = "Employee works number";
    private const string PaymentHeaderFormat = "Payment {0} - {1}";
    private const string FieldDescription = "Description";
    private const string FieldNormalHours = "Number of normal hours";
    private const string EmptyField = "";

    private static readonly string[] HeaderFields = BuildHeader();

    public string FormatKey => PayrollExportConstants.FormatKeyBrightpayIeUk;

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
                    case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
                    case PayrollEntryKind.Surcharge when entry.Unit != PayrollQuantityUnit.Hours:
                        counter.UnsupportedUnits++;
                        continue;

                    case PayrollEntryKind.WorkHours:
                        if (!TryAddToTotal(descriptions, totals, config.BaseWageType, entry.Quantity))
                        {
                            counter.UnmappedBaseWages++;
                            continue;
                        }

                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (!TryAddToTotal(descriptions, totals, config.SurchargeWageType, entry.Quantity))
                        {
                            counter.UnmappedSurcharges++;
                            continue;
                        }

                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                        {
                            counter.UnmappedAbsences++;
                            continue;
                        }

                        if (entry.Unit != PayrollQuantityUnit.Hours)
                        {
                            counter.UnsupportedUnits++;
                            continue;
                        }

                        if (!TryAddToTotal(descriptions, totals, mapping.WageType, entry.Quantity))
                        {
                            counter.UnmappedAbsences++;
                            continue;
                        }

                        counter.Emitted++;
                        break;

                    default:
                        counter.UnsupportedKinds++;
                        continue;
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
            fields.Add(string.Format(CultureInfo.InvariantCulture, PaymentHeaderFormat, block, FieldNormalHours));
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
