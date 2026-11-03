// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats employee-centric payroll data as a Merit Palk (Estonia) "Tasude import" CSV
/// (12 fixed positional columns A–L, semicolon delimiter, comma decimal separator, UTF-8 without BOM,
/// one header row with the spec's column names, CRLF line endings).
/// Column order: A employee personal ID or contract import code; B free text (employee name);
/// C free text; D wage/deduction import code; E rate or amount; F quantity (hours);
/// G mandatory empty column; H department code; I cost center code; J project code;
/// K standard hours; L standard days.
/// One row is written per employee and wage/deduction import code, carrying the hours summed over the period.
/// </summary>
/// <remarks>
/// Spec sources (verified 2026-11-02):
/// "Tasude ja kinnipidamiste import failist Merit Palgas", Merit Tarkvara, May 2023,
/// https://www.merit.ee/juhend/muud-palk/merit-palga-tasude-import-formaadi-kirjeldus.pdf, and the English help-center
/// article https://support.merit.ee/en/articles/455154-salaries-and-withholdings-import-from-file-in-merit-palk.
/// Verified against the spec: "Kodeering: UTF-8"; "Andmeväljade eraldaja: semikoolon" (the delimiter is therefore
/// fixed and config.Delimiter / config.Encoding are ignored); "Murdosa eraldaja arvudes: koma"; the 12 columns and
/// their order; A and D mandatory; G "kohustuslik tühi veerg"; at least one of E/F filled; F = hours for hourly and
/// monthly wages; columns from M on are ignored. The spec requires column names in the first row "kui ei impordita
/// korraga kõiki andmeid" (when not all columns are filled) — this formatter never fills E and H–L, so it always
/// writes a header row; the spec prescribes no exact header text, so the spec's own Estonian column labels are used.
/// The layout has no date column: the import targets the wage sheet of the period selected in the UI. The spec does
/// not say whether repeated rows for the same employee and code are summed or replace each other, so entries are
/// aggregated per employee and import code (summed exactly, then rounded once to 2 decimals, midpoint away from
/// zero; the spec does not state a precision). Numbers are written with a fixed invariant-based format (comma
/// decimal, ASCII minus, no grouping) instead of the et-EE culture, whose ICU data uses U+2212 as the minus sign
/// and depends on ICU being present. Rows with an empty import code are never written because D is mandatory:
/// worked hours without BaseWageType count as SkippedUnmappedBaseWageCount, surcharges without SurchargeWageType as
/// SkippedUnmappedSurchargeCount, unmapped or empty-mapped absences as SkippedAbsenceCount. Day-based entries
/// (PayrollQuantityUnit.Days) have no place in F (hours) — column L is the standard-days norm, not actual days — and
/// are counted in SkippedUnsupportedUnitCount. Not covered by the spec and left as-is: BOM (none written), line
/// endings (CRLF), quoting (none; CR/LF and ';' are stripped from values). Known limitations: column A carries the
/// Klacks IdNumber, which Merit only matches if it was entered as the contract import code ("lepingu impordi kood")
/// in Merit — the domain model carries no Estonian personal ID code; column E stays empty because the model carries
/// no monetary amount, so fixed-amount wage types ("Summa x Kogus x Koefitsient") are not supported. Deduction codes
/// must carry a leading minus in the file; the formatter does not add it, so the value stored in AbsenceMappingJson
/// must already include the sign.
/// AbsenceMappingJson accepts both the Merit-specific plain-string shape {"&lt;absenceId&gt;":"PIK"} and the common
/// payroll mapping shape {"&lt;absenceId&gt;":{"WageType":"PIK","Ausfallschluessel":"…"}} (WageType is matched
/// case-insensitively, other properties are ignored); any other JSON value (number, array, null) or an empty or
/// whitespace code counts the absence as SkippedAbsenceCount.
/// </remarks>
using System.Globalization;
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class MeritPalkEeExportFormatter : IPayrollExportFormatter
{
    private const string Delimiter = ";";
    private const string DecimalSeparator = ",";
    private const string QuantityFormat = "0.00";
    private const int QuantityDecimals = 2;
    private const string EmptyField = "";
    private const string MappingWageTypeProperty = "WageType";

    private const int ColumnEmployeeCode = 0;
    private const int ColumnEmployeeName = 1;
    private const int ColumnImportCode = 3;
    private const int ColumnQuantity = 5;

    private static readonly string[] HeaderColumns =
    [
        "töötaja isikukood või lepingu impordi kood",
        "vaba sisuga veerg",
        "vaba sisuga veerg",
        "tasuliigi või kinnipidamise impordi kood",
        "määr või summa",
        "kogus",
        "kohustuslik tühi veerg",
        "osakonna kood",
        "kulukoha kood",
        "projekti kood",
        "normtundide arv",
        "normpäevade arv",
    ];

    private static readonly NumberFormatInfo QuantityNumberFormat = CreateQuantityNumberFormat();

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public string FormatKey => PayrollExportConstants.FormatKeyMeritPalkEe;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<JsonElement>(config.AbsenceMappingJson, counter);

        var sb = new StringBuilder();
        AppendFields(sb, HeaderColumns);
        var recordCount = 0;

        foreach (var employee in data.Employees)
        {
            var importCodeOrder = new List<string>();
            var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);

            foreach (var entry in employee.Entries)
            {
                var importCode = ResolveImportCode(entry, config, absenceMapping, counter);
                if (importCode == null)
                {
                    continue;
                }

                if (!totals.ContainsKey(importCode))
                {
                    importCodeOrder.Add(importCode);
                    totals[importCode] = 0m;
                }

                totals[importCode] += entry.Quantity;
                counter.Emitted++;
            }

            var personnelCode = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var importCode in importCodeOrder)
            {
                AppendLine(sb, personnelCode, employee.FullName, importCode, FormatQuantity(totals[importCode]));
                recordCount++;
            }
        }

        return counter.ToResult(Utf8WithoutBom.GetBytes(sb.ToString()), recordCount);
    }

    private static string? ResolveImportCode(
        PayrollDayEntry entry,
        PayrollExportGroupConfig config,
        Dictionary<string, JsonElement> absenceMapping,
        PayrollExportSkipCounter counter)
    {
        switch (entry.Kind)
        {
            case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
            case PayrollEntryKind.Surcharge when entry.Unit != PayrollQuantityUnit.Hours:
                counter.UnsupportedUnits++;
                return null;

            case PayrollEntryKind.WorkHours:
                if (string.IsNullOrEmpty(Sanitize(config.BaseWageType)))
                {
                    counter.UnmappedBaseWages++;
                    return null;
                }

                return Sanitize(config.BaseWageType);

            case PayrollEntryKind.Surcharge:
                if (string.IsNullOrEmpty(Sanitize(config.SurchargeWageType)))
                {
                    counter.UnmappedSurcharges++;
                    return null;
                }

                return Sanitize(config.SurchargeWageType);

            case PayrollEntryKind.Absence:
                var key = entry.AbsenceId?.ToString();
                var importCode = key != null && absenceMapping.TryGetValue(key, out var mapping)
                    ? Sanitize(ResolveMappedCode(mapping))
                    : EmptyField;
                if (string.IsNullOrEmpty(importCode))
                {
                    counter.UnmappedAbsences++;
                    return null;
                }

                if (entry.Unit != PayrollQuantityUnit.Hours)
                {
                    counter.UnsupportedUnits++;
                    return null;
                }

                return importCode;

            default:
                counter.UnsupportedKinds++;
                return null;
        }
    }

    private static string? ResolveMappedCode(JsonElement mapping)
    {
        switch (mapping.ValueKind)
        {
            case JsonValueKind.String:
                return mapping.GetString();

            case JsonValueKind.Object:
                foreach (var property in mapping.EnumerateObject())
                {
                    if (string.Equals(property.Name, MappingWageTypeProperty, StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        return property.Value.GetString();
                    }
                }

                return null;

            default:
                return null;
        }
    }

    private static void AppendLine(
        StringBuilder sb,
        string personnelCode,
        string employeeName,
        string importCode,
        string quantity)
    {
        var fields = new string[PayrollExportConstants.MeritPalkFieldCount];
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = EmptyField;
        }

        fields[ColumnEmployeeCode] = Sanitize(personnelCode);
        fields[ColumnEmployeeName] = Sanitize(employeeName);
        fields[ColumnImportCode] = importCode;
        fields[ColumnQuantity] = quantity;

        AppendFields(sb, fields);
    }

    private static void AppendFields(StringBuilder sb, string[] fields)
    {
        sb.Append(string.Join(Delimiter, fields));
        sb.Append(PayrollExportConstants.LineEnding);
    }

    private static string FormatQuantity(decimal value)
    {
        var rounded = Math.Round(value, QuantityDecimals, MidpointRounding.AwayFromZero);
        return rounded.ToString(QuantityFormat, QuantityNumberFormat);
    }

    private static NumberFormatInfo CreateQuantityNumberFormat()
    {
        var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        format.NumberDecimalSeparator = DecimalSeparator;
        format.NumberGroupSeparator = EmptyField;
        return NumberFormatInfo.ReadOnly(format);
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
            .Replace(Delimiter, EmptyField)
            .Trim();
    }
}
