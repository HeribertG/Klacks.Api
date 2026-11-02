// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats employee-centric payroll data for KLARA Lohn (KLARA Business AG, Switzerland) as a semicolon-separated
/// transfer list of variable salary items ("variable Lohndaten": hours per Lohnart) for the payslips of the closed
/// period. Seven columns with one German header row: Personalnummer; Nachname; Vorname; Periode (yyyy-MM);
/// Lohnart; Menge; Einheit (Stunden or Tage). One row is written per employee, calendar month, Lohnart and unit,
/// carrying the quantity summed over that month. Fixed layout: ';' delimiter, '.' decimal separator with two
/// decimals and no thousands separator, UTF-8 with BOM, CRLF line endings.
/// </summary>
/// <remarks>
/// IMPORTANT - this is NOT a KLARA-specified import format. Research (2026-11-02) found no documented file import
/// for payroll entries in KLARA Lohn. What KLARA documents instead:
/// - Hourly-wage hours are entered manually in the "Übersicht" menu or directly on the payslip
///   (https://support.klara.ch/hc/de/articles/360000606305-Mitarbeiter-arbeitet-im-Stundenlohn-Wie-k%C3%B6nnen-die-Stunden-erfasst-werden,
///   https://support.klara.ch/hc/en-us/articles/360011225839-First-steps-employees-and-salaries).
/// - Variable salary components are added in the payroll run with the "Lohnart hinzufügen" button
///   (https://www.klara.ch/blog/neuer-lohnlauf-klara, https://www.klara.ch/en/blog/new-payroll-run-klara).
/// - The official machine path is the KLARA API, use case "Variable Lohndaten übergeben" - "Übergib Stunden,
///   Zulagen und Prämien direkt an die Lohnabrechnung deiner Mitarbeitenden" (https://www.klara.ch/api,
///   https://www.klara.ch/en/api), documented under https://developer.klara.ch/#payroll, and the KLARA MCP tools
///   klara_addable_salary_items / klara_prepare_salary_item ("Record a variable salary item (hours, allowances,
///   bonuses) on an open payslip") and klara_payroll_file_check ("Check the payroll interface file - format and
///   size only") (https://mcp.klara.ch/docs?lang=en). The MCP page mentions a "payroll interface file" but does not
///   describe its format; developer.klara.ch and the OpenAPI document of api.klara.ch could not be read during
///   research, so the payroll endpoint fields and that file's layout are unverified.
/// Consequently every layout decision below is a Klacks design choice, not a KLARA requirement: the column names
/// are the German terms KLARA's UI and Swiss payroll practice use (Personalnummer, Lohnart, Menge), not confirmed
/// import field names; the file is meant as a reviewable, Excel-friendly transfer list for manual entry or for a
/// later API-based push, not for a KLARA import wizard. The layout mirrors the information the API/MCP salary item
/// carries according to the use-case text (employee, salary type, quantity on the open payslip of a month).
/// Design choices: delimiter and encoding are fixed (config.Delimiter / config.Encoding are ignored) - ';' is the
/// list separator of Swiss-German Excel, '.' is the Swiss (de-CH) decimal separator, and UTF-8 with BOM lets Excel
/// show umlauts in names correctly. Because KLARA payslips are monthly and a salary item carries no day, entries are
/// aggregated per employee, calendar month of the entry date (Periode), Lohnart and unit, in first-seen order;
/// quantities are summed exactly and rounded once to 2 decimals, midpoint away from zero. Hours and days are never
/// mixed in one row: Einheit states the unit, and the Lohnart must be of that unit in KLARA - a day-based entry
/// (on-call duty) is written as its day count, never as an hour equivalent. Personalnummer is the Klacks IdNumber,
/// which only matches if the same number is maintained as personnel number in KLARA.
/// Skip accounting (every entry is emitted or counted exactly once): worked hours without BaseWageType count as
/// SkippedUnmappedBaseWageCount, surcharges without SurchargeWageType as SkippedUnmappedSurchargeCount, absences
/// without an id, without a mapping or with an empty mapped WageType as SkippedAbsenceCount, day-based worked hours
/// and surcharges (no day-based base/surcharge Lohnart is configurable) as SkippedUnsupportedUnitCount, unknown
/// entry kinds as SkippedUnsupportedKindCount. Text values are stripped of CR, LF and ';' and passed through
/// CsvFormulaGuard because the file is expected to be opened in a spreadsheet; no quoting is used.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class KlaraChExportFormatter : IPayrollExportFormatter
{
    private const string Delimiter = ";";
    private const string QuantityFormat = "0.00";
    private const int QuantityDecimals = 2;
    private const string PeriodFormat = "yyyy-MM";
    private const string EmptyField = "";
    private const string UnitHours = "Stunden";
    private const string UnitDays = "Tage";

    private static readonly string[] HeaderColumns =
    [
        "Personalnummer",
        "Nachname",
        "Vorname",
        "Periode",
        "Lohnart",
        "Menge",
        "Einheit",
    ];

    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public string FormatKey => PayrollExportConstants.FormatKeyKlaraCh;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PayrollAbsenceMapping>(config.AbsenceMappingJson, counter);

        var sb = new StringBuilder();
        AppendFields(sb, HeaderColumns);
        var recordCount = 0;

        foreach (var employee in data.Employees)
        {
            var rowOrder = new List<RowKey>();
            var totals = new Dictionary<RowKey, decimal>();

            foreach (var entry in employee.Entries)
            {
                var wageType = ResolveWageType(entry, config, absenceMapping, counter);
                if (wageType == null)
                {
                    continue;
                }

                var key = new RowKey(
                    entry.Date.ToString(PeriodFormat, CultureInfo.InvariantCulture),
                    wageType,
                    entry.Unit);

                if (!totals.ContainsKey(key))
                {
                    rowOrder.Add(key);
                    totals[key] = 0m;
                }

                totals[key] += entry.Quantity;
                counter.Emitted++;
            }

            var personnelNumber = employee.IdNumber.ToString(CultureInfo.InvariantCulture);
            var lastName = SanitizeText(employee.LastName);
            var firstName = SanitizeText(employee.FirstName);

            foreach (var key in rowOrder)
            {
                AppendFields(
                    sb,
                    [
                        personnelNumber,
                        lastName,
                        firstName,
                        key.Period,
                        key.WageType,
                        FormatQuantity(totals[key]),
                        key.Unit == PayrollQuantityUnit.Days ? UnitDays : UnitHours,
                    ]);
                recordCount++;
            }
        }

        return counter.ToResult(Encode(sb.ToString()), recordCount);
    }

    private static string? ResolveWageType(
        PayrollDayEntry entry,
        PayrollExportGroupConfig config,
        Dictionary<string, PayrollAbsenceMapping> absenceMapping,
        PayrollExportSkipCounter counter)
    {
        switch (entry.Kind)
        {
            case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
            case PayrollEntryKind.Surcharge when entry.Unit != PayrollQuantityUnit.Hours:
                counter.UnsupportedUnits++;
                return null;

            case PayrollEntryKind.WorkHours:
                var baseWageType = SanitizeText(config.BaseWageType);
                if (string.IsNullOrEmpty(baseWageType))
                {
                    counter.UnmappedBaseWages++;
                    return null;
                }

                return baseWageType;

            case PayrollEntryKind.Surcharge:
                var surchargeWageType = SanitizeText(config.SurchargeWageType);
                if (string.IsNullOrEmpty(surchargeWageType))
                {
                    counter.UnmappedSurcharges++;
                    return null;
                }

                return surchargeWageType;

            case PayrollEntryKind.Absence:
                var key = entry.AbsenceId?.ToString();
                if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                {
                    counter.UnmappedAbsences++;
                    return null;
                }

                var absenceWageType = SanitizeText(mapping?.WageType);
                if (string.IsNullOrEmpty(absenceWageType))
                {
                    counter.UnmappedAbsences++;
                    return null;
                }

                return absenceWageType;

            default:
                counter.UnsupportedKinds++;
                return null;
        }
    }

    private static void AppendFields(StringBuilder sb, string[] fields)
    {
        sb.Append(string.Join(Delimiter, fields));
        sb.Append(PayrollExportConstants.LineEnding);
    }

    private static string FormatQuantity(decimal value)
    {
        var rounded = Math.Round(value, QuantityDecimals, MidpointRounding.AwayFromZero);
        return rounded.ToString(QuantityFormat, CultureInfo.InvariantCulture);
    }

    private static string SanitizeText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return EmptyField;
        }

        var stripped = value
            .Replace("\r", EmptyField)
            .Replace("\n", EmptyField)
            .Replace(Delimiter, EmptyField)
            .Trim();

        return CsvFormulaGuard.Neutralize(stripped);
    }

    private static byte[] Encode(string content)
    {
        var preamble = Utf8WithBom.GetPreamble();
        var body = Utf8WithBom.GetBytes(content);
        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }

    private readonly record struct RowKey(string Period, string WageType, PayrollQuantityUnit Unit);
}
