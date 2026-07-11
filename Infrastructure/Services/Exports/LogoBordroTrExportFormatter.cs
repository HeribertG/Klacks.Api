// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports payroll data as a Logo Bordro Plus (Turkey) "Puantaj" Excel import template (payroll
/// family, key "logo-bordro-tr"): a single-sheet .xlsx with a three-row structure — row 1 human
/// header labels, row 2 the five-digit reference codes that map each column to a Logo payroll field,
/// row 3+ one row per employee. Identity columns Sicil No / Ad / Soyad are followed by one column per
/// distinct configured wage-type code, whose cells hold that employee's aggregated hours per wage type.
/// </summary>
/// <remarks>
/// The row-2 reference codes are Logo's import key: without the correct codes Logo cannot map the
/// columns. Klacks does not know a tenant's Logo codes, so this formatter never fabricates them:
/// - Wage-type column codes come straight from the group config (BaseWageType, SurchargeWageType and
///   each absence mapping's WageType), exactly as the other payroll formatters treat those strings.
/// - The Sicil No column uses the documented standard code "00001"; the Ad and Soyad column codes are
///   left EMPTY for the customer to fill in from their own Logo installation (they were not verifiable
///   against a live Logo source — the official Logo import docs are defunct and only partner blogs
///   describe the layout). This produces a structurally correct template that the customer completes
///   with their installation-specific codes before importing, rather than a plausible-but-wrong file
///   with invented codes.
/// Only wage types with a non-empty code produce a column; work-hours/surcharge entries whose code is
/// empty are dropped, and absence entries with no mapping are counted in SkippedAbsenceCount. Hours are
/// aggregated per employee per wage type and written as real Excel numbers. Dates use the DD.MM.YYYY
/// Turkish convention where a date is emitted, though this pivot template is period-summary, not daily.
/// </remarks>
using System.Text.Json;
using ClosedXML.Excel;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class LogoBordroTrExportFormatter : IPayrollExportFormatter
{
    private const string SheetName = "Puantaj";
    private const int HeaderRowNumber = 1;
    private const int CodeRowNumber = 2;
    private const int FirstDataRowNumber = 3;
    private const string HeaderSicilNo = "Sicil No";
    private const string HeaderAd = "Ad";
    private const string HeaderSoyad = "Soyad";
    private const string SicilNoCode = "00001";
    private const string EmptyCode = "";
    private const char NameSeparator = ' ';

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyLogoBordroTr;

    public string ContentType => ExportConstants.ContentTypeXlsx;

    public string FileExtension => ".xlsx";

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var absenceMapping = ParseAbsenceMapping(config.AbsenceMappingJson);
        var wageCodes = BuildWageCodeColumns(config, absenceMapping);
        var wageColumnIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < wageCodes.Count; i++)
        {
            wageColumnIndex[wageCodes[i]] = FirstDataColumn + i;
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        WriteHeaderAndCodeRows(sheet, wageCodes);

        var row = FirstDataRowNumber;
        var recordCount = 0;
        var skippedAbsenceCount = 0;

        foreach (var employee in data.Employees)
        {
            var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);

            foreach (var entry in employee.Entries)
            {
                var code = ResolveWageCode(entry, config, absenceMapping, ref skippedAbsenceCount);
                if (string.IsNullOrEmpty(code))
                {
                    continue;
                }

                totals.TryGetValue(code, out var current);
                totals[code] = current + entry.Quantity;
            }

            WriteEmployeeRow(sheet, row, employee, totals, wageColumnIndex);
            row++;
            recordCount++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new PayrollExportResult
        {
            Content = stream.ToArray(),
            RecordCount = recordCount,
            SkippedAbsenceCount = skippedAbsenceCount,
        };
    }

    private const int SicilNoColumn = 1;
    private const int AdColumn = 2;
    private const int SoyadColumn = 3;
    private const int FirstDataColumn = 4;

    private static void WriteHeaderAndCodeRows(IXLWorksheet sheet, IReadOnlyList<string> wageCodes)
    {
        sheet.Cell(HeaderRowNumber, SicilNoColumn).Value = HeaderSicilNo;
        sheet.Cell(HeaderRowNumber, AdColumn).Value = HeaderAd;
        sheet.Cell(HeaderRowNumber, SoyadColumn).Value = HeaderSoyad;

        sheet.Cell(CodeRowNumber, SicilNoColumn).Value = SicilNoCode;
        sheet.Cell(CodeRowNumber, AdColumn).Value = EmptyCode;
        sheet.Cell(CodeRowNumber, SoyadColumn).Value = EmptyCode;

        for (var i = 0; i < wageCodes.Count; i++)
        {
            var column = FirstDataColumn + i;
            sheet.Cell(HeaderRowNumber, column).Value = wageCodes[i];
            sheet.Cell(CodeRowNumber, column).Value = wageCodes[i];
        }
    }

    private static void WriteEmployeeRow(
        IXLWorksheet sheet,
        int row,
        PayrollEmployee employee,
        Dictionary<string, decimal> totals,
        Dictionary<string, int> wageColumnIndex)
    {
        var (ad, soyad) = SplitName(employee.FullName);

        sheet.Cell(row, SicilNoColumn).Value = employee.IdNumber;
        sheet.Cell(row, AdColumn).Value = ad;
        sheet.Cell(row, SoyadColumn).Value = soyad;

        foreach (var (code, total) in totals)
        {
            if (wageColumnIndex.TryGetValue(code, out var column))
            {
                sheet.Cell(row, column).Value = total;
            }
        }
    }

    private static string ResolveWageCode(
        PayrollDayEntry entry,
        PayrollExportGroupConfig config,
        Dictionary<string, PayrollAbsenceMapping> absenceMapping,
        ref int skippedAbsenceCount)
    {
        switch (entry.Kind)
        {
            case PayrollEntryKind.WorkHours:
                return config.BaseWageType;

            case PayrollEntryKind.Surcharge:
                return config.SurchargeWageType;

            case PayrollEntryKind.Absence:
                var key = entry.AbsenceId?.ToString();
                if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                {
                    skippedAbsenceCount++;
                    return EmptyCode;
                }

                return mapping.WageType;

            default:
                return EmptyCode;
        }
    }

    private static List<string> BuildWageCodeColumns(
        PayrollExportGroupConfig config,
        Dictionary<string, PayrollAbsenceMapping> absenceMapping)
    {
        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string? code)
        {
            if (!string.IsNullOrEmpty(code) && seen.Add(code))
            {
                ordered.Add(code);
            }
        }

        Add(config.BaseWageType);
        Add(config.SurchargeWageType);
        foreach (var mapping in absenceMapping.Values)
        {
            Add(mapping.WageType);
        }

        return ordered;
    }

    private static (string Ad, string Soyad) SplitName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (string.Empty, string.Empty);
        }

        var trimmed = fullName.Trim();
        var lastSpace = trimmed.LastIndexOf(NameSeparator);
        if (lastSpace <= 0)
        {
            return (trimmed, string.Empty);
        }

        return (trimmed[..lastSpace].Trim(), trimmed[(lastSpace + 1)..].Trim());
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
}
