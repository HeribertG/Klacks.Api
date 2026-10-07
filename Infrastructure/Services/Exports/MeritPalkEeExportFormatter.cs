// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats employee-centric payroll data as a Merit Palk (Estonia) "Tasude import" CSV
/// (12 fixed positional fields, semicolon delimiter, UTF-8 encoding, no header row).
/// Field order: Employee personal ID or contract import code; Employee name (free text);
/// Reserved free text; Wage/deduction import code; Rate or amount; Quantity (hours);
/// Reserved (must stay empty); Department code; Cost center code; Project code;
/// Standard hours; Standard days.
/// </summary>
/// <remarks>
/// Source: "Tasude ja kinnipidamiste import failist Merit Palgas" (Merit Tarkvara, May 2023).
/// The spec's 12 columns do NOT include a per-row date — Merit Palk's import targets a wage
/// sheet for a period that is already selected in the UI, not a per-day booking. This formatter
/// still emits one row per PayrollDayEntry (per the day-granular domain model) rather than
/// aggregating per employee/wage-code across the period; whether Merit Palk sums duplicate
/// wage-code rows correctly within a single import is not stated in the spec and is unverified.
/// Fields 1 (employee code) and 4 (wage/deduction import code) are always mandatory; field 7 is
/// mandatory-empty. The spec also marks fields 5 (rate/amount) and 6 (quantity) as required: Merit
/// derives the amount for time-based wage types from the contract rate x quantity, so a filled
/// field 6 alone is accepted for those, but fixed-amount wage types ("Summa x Kogus x Koefitsient")
/// reject a row whose field 5 is empty. This formatter always fills field 6 (quantity/hours) and
/// leaves field 5 empty because the domain model carries no monetary amount — a known limitation
/// that is correct for time-based wage types and unsupported for fixed-amount ones. For
/// deductions (kinnipidamised), the spec requires the import code to carry a leading minus sign
/// in the file even though the wage-type card shows it as positive; this formatter does not add
/// the sign itself — the value stored per absence in AbsenceMappingJson must already include the
/// correct sign, consistent with how BaseWageType/SurchargeWageType are stored as literal
/// tenant-specific values. Rows for worked hours are always emitted (base wage type may be an
/// empty placeholder until configured); surcharge rows are emitted only when a surcharge wage
/// type is configured; absence rows are emitted only when the absence is mapped — unmapped
/// absences are counted in SkippedAbsenceCount instead of being dropped silently. Field 6 carries hours, so a
/// day-based entry (PayrollQuantityUnit.Days, on-call duty) has no correct place in this layout: it is not emitted
/// and counted in SkippedUnsupportedUnitCount, never written as one hour.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class MeritPalkEeExportFormatter : IPayrollExportFormatter
{
    private const string QuantityFormat = "F2";
    private const string EstonianCulture = "et-EE";
    private const string EmptyField = "";

    public string FormatKey => PayrollExportConstants.FormatKeyMeritPalkEe;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var delimiter = string.IsNullOrEmpty(config.Delimiter)
            ? PayrollExportConstants.DefaultDelimiter
            : config.Delimiter;
        var culture = CultureInfo.GetCultureInfo(EstonianCulture);
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<string>(config.AbsenceMappingJson, counter);

        var sb = new StringBuilder();

        foreach (var employee in data.Employees)
        {
            var personnelCode = employee.IdNumber.ToString(CultureInfo.InvariantCulture);
            var employeeName = employee.FullName;

            foreach (var entry in employee.Entries)
            {
                var quantity = entry.Quantity.ToString(QuantityFormat, culture);

                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
                    case PayrollEntryKind.Surcharge when entry.Unit != PayrollQuantityUnit.Hours:
                        counter.UnsupportedUnits++;
                        continue;

                    case PayrollEntryKind.WorkHours:
                        AppendLine(sb, delimiter, personnelCode, employeeName, config.BaseWageType, quantity);
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (string.IsNullOrEmpty(config.SurchargeWageType))
                        {
                            counter.UnmappedSurcharges++;
                            continue;
                        }

                        AppendLine(sb, delimiter, personnelCode, employeeName, config.SurchargeWageType, quantity);
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var importCode))
                        {
                            counter.UnmappedAbsences++;
                            continue;
                        }

                        if (entry.Unit != PayrollQuantityUnit.Hours)
                        {
                            counter.UnsupportedUnits++;
                            continue;
                        }

                        AppendLine(sb, delimiter, personnelCode, employeeName, importCode, quantity);
                        counter.Emitted++;
                        break;

                    default:
                        counter.UnsupportedKinds++;
                        continue;
                }
            }
        }

        return counter.ToResult(Encoding.UTF8.GetBytes(sb.ToString()), counter.Emitted);
    }

    private static void AppendLine(
        StringBuilder sb,
        string delimiter,
        string personnelCode,
        string employeeName,
        string wageImportCode,
        string quantity)
    {
        var fields = new string[PayrollExportConstants.MeritPalkFieldCount];
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = EmptyField;
        }

        fields[0] = Sanitize(personnelCode, delimiter);
        fields[1] = Sanitize(employeeName, delimiter);
        fields[3] = Sanitize(wageImportCode, delimiter);
        fields[5] = Sanitize(quantity, delimiter);

        sb.Append(string.Join(delimiter, fields));
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
}
