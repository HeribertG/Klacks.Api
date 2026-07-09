// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Formats employee-centric payroll data as a WinMENTOR (Romania) "Import pontaje" workbook,
/// structura 2 (daily timesheet): sheet "pontaj", one row per employee spanning the whole export
/// period, with An/Luna/Simbol formatie/Identificator formatie/Marca identity columns, one column
/// per calendar day of the period, and two four-column summary blocks (AVANS / LICHIDARE) each with
/// "zile lucrate", "ore suplim.I", "ore supllim.II" and "ore noapte" sub-columns, followed by an
/// "OBSERVATII:" column. Data rows start on row 3, matching the vendor sample's own instruction
/// ("linia 3 coloana 1").
/// </summary>
/// <remarks>
/// Verified against the actual public WinMENTOR sample workbook (downloaded and parsed cell-by-cell,
/// not just described in docs): http://download.winmentor.ro/WinMentor/Documentatie/03_SALARII/Solutii/Import%20pontaje%20structura%202%20exemplu.xls
/// Sheet name, Windows-1252 codepage, "linia 3 coloana 1" data start, the 5 identity columns, the
/// 31 day-of-month columns and the AVANS/LICHIDARE 4-field blocks (incl. the vendor's own
/// "supllim.II" and "Identi ficator formatie" spelling, preserved verbatim here for fidelity) were
/// all confirmed this way. This corrects docs/knowledge/export-samples/findings-ch-pl-hu-gr-ro-balkan-ie-uk.md
/// section 5 (RO), whose prose summary omitted the day-of-month columns entirely.
/// The accompanying spec PDFs are scanned images with no text layer, so the full absence-code
/// catalogue (concediu/boala codes) could not be verified; AbsenceMappingJson.Ausfallschluessel is
/// therefore treated as an opaque, tenant-supplied WinMENTOR code and written as-is.
/// Known simplifications, all deliberate MVP scope cuts:
/// (1) File is written as .xlsx via ClosedXML, not the vendor's original .xls (OLE2/BIFF) — modern
/// WinMENTOR versions generally accept .xlsx, but this is an assumption, not a confirmed fact.
/// (2) "Simbol formatie" / "Identi ficator formatie" (department code) are left blank: Klacks'
/// PayrollExportGroupConfig has no per-employee department-code field to source them from.
/// (3) Surcharge entries are added into the same day's numeric quantity, exactly like the other
/// Tier-A/Tier-B formatters do when the target has no dedicated slot; the AVANS/LICHIDARE
/// "ore suplim.I" / "ore supllim.II" / "ore noapte" sub-columns are always left blank because
/// PayrollDayEntry has no overtime-tier or night-hours distinction to source them from.
/// (4) The AVANS (first half) / LICHIDARE (second half) split point is a documented business-
/// convention assumption (day-of-month &lt;= 15 vs &gt; 15, matching the common Romanian bi-monthly
/// advance/settlement cycle), not something read from an authoritative WinMENTOR spec.
/// (5) A day with a mapped Absence entry writes the absence code instead of hours; an unmapped
/// Absence is counted in SkippedAbsenceCount (never dropped silently) and the day cell falls back
/// to that day's summed WorkHours/Surcharge quantity instead.
/// (6) RecordCount here counts emitted employee rows (one row = the whole period), not individual
/// entries, because this format is a per-employee calendar matrix rather than one row per entry.
/// (7) Assumes the export period is a single calendar month; An/Luna are derived from StartDate and
/// day headers are day-of-month numbers, so a multi-month period would produce misleading repeated
/// day-of-month headers without raising an error.
/// </remarks>
using System.Text.Json;
using ClosedXML.Excel;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class WinmentorRoExportFormatter : IPayrollExportFormatter
{
    private const string SheetName = "pontaj";
    private const string HeaderYear = "An";
    private const string HeaderMonth = "Luna";
    private const string HeaderDepartmentSymbol = "Simbol formatie";
    private const string HeaderDepartmentId = "Identi ficator formatie";
    private const string HeaderPersonnelNumber = "Marca";
    private const string HeaderAvans = "AVANS";
    private const string HeaderLichidare = "LICHIDARE";
    private const string HeaderWorkedDays = "zile lucrate";
    private const string HeaderOvertimeTierOne = "ore suplim.I";
    private const string HeaderOvertimeTierTwo = "ore supllim.II";
    private const string HeaderNightHours = "ore noapte";
    private const string HeaderObservations = "OBSERVATII:";

    private const int IdentityColumnCount = 5;
    private const int SummaryBlockColumnCount = 4;
    private const int HeaderRow = 1;
    private const int SubHeaderRow = 2;
    private const int FirstDataRow = 3;
    private const int AvansHalfMonthLastDay = 15;

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyWinmentorRo;

    public string ContentType => ExportConstants.ContentTypeXlsx;

    public string FileExtension => ".xlsx";

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var absenceMapping = ParseAbsenceMapping(config.AbsenceMappingJson);
        var dates = GetPeriodDates(data);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        var avansStartColumn = IdentityColumnCount + dates.Count + 1;
        var lichidareStartColumn = avansStartColumn + SummaryBlockColumnCount;
        var observationsColumn = lichidareStartColumn + SummaryBlockColumnCount;

        WriteHeader(sheet, dates, avansStartColumn, lichidareStartColumn, observationsColumn);

        var row = FirstDataRow;
        var recordCount = 0;
        var skippedAbsenceCount = 0;

        foreach (var employee in data.Employees)
        {
            var entriesByDate = employee.Entries.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => g.ToList());

            sheet.Cell(row, 1).Value = data.StartDate.Year;
            sheet.Cell(row, 2).Value = data.StartDate.Month;
            sheet.Cell(row, IdentityColumnCount).Value = employee.IdNumber;

            var avansWorkedDays = 0;
            var lichidareWorkedDays = 0;

            for (var i = 0; i < dates.Count; i++)
            {
                var date = dates[i];
                var column = IdentityColumnCount + 1 + i;
                entriesByDate.TryGetValue(date, out var dayEntries);

                var (absenceCode, hours, isWorkedDay, absenceSkipped) = ResolveDayCell(dayEntries, absenceMapping);

                if (absenceSkipped)
                {
                    skippedAbsenceCount++;
                }

                if (absenceCode is not null)
                {
                    sheet.Cell(row, column).Value = absenceCode;
                }
                else if (hours.HasValue)
                {
                    sheet.Cell(row, column).Value = hours.Value;
                }

                if (isWorkedDay)
                {
                    if (date.Day <= AvansHalfMonthLastDay)
                    {
                        avansWorkedDays++;
                    }
                    else
                    {
                        lichidareWorkedDays++;
                    }
                }
            }

            sheet.Cell(row, avansStartColumn).Value = avansWorkedDays;
            sheet.Cell(row, lichidareStartColumn).Value = lichidareWorkedDays;

            recordCount++;
            row++;
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

    private static void WriteHeader(
        IXLWorksheet sheet,
        List<DateOnly> dates,
        int avansStartColumn,
        int lichidareStartColumn,
        int observationsColumn)
    {
        sheet.Cell(HeaderRow, 1).Value = HeaderYear;
        sheet.Cell(HeaderRow, 2).Value = HeaderMonth;
        sheet.Cell(HeaderRow, 3).Value = HeaderDepartmentSymbol;
        sheet.Cell(HeaderRow, 4).Value = HeaderDepartmentId;
        sheet.Cell(HeaderRow, IdentityColumnCount).Value = HeaderPersonnelNumber;

        for (var i = 0; i < IdentityColumnCount; i++)
        {
            sheet.Range(HeaderRow, i + 1, SubHeaderRow, i + 1).Merge();
        }

        for (var i = 0; i < dates.Count; i++)
        {
            var column = IdentityColumnCount + 1 + i;
            sheet.Cell(HeaderRow, column).Value = dates[i].Day;
            sheet.Range(HeaderRow, column, SubHeaderRow, column).Merge();
        }

        WriteSummaryBlockHeader(sheet, avansStartColumn, HeaderAvans);
        WriteSummaryBlockHeader(sheet, lichidareStartColumn, HeaderLichidare);

        sheet.Cell(SubHeaderRow, observationsColumn).Value = HeaderObservations;
    }

    private static void WriteSummaryBlockHeader(IXLWorksheet sheet, int startColumn, string blockHeader)
    {
        sheet.Cell(HeaderRow, startColumn).Value = blockHeader;
        sheet.Range(HeaderRow, startColumn, HeaderRow, startColumn + SummaryBlockColumnCount - 1).Merge();

        sheet.Cell(SubHeaderRow, startColumn).Value = HeaderWorkedDays;
        sheet.Cell(SubHeaderRow, startColumn + 1).Value = HeaderOvertimeTierOne;
        sheet.Cell(SubHeaderRow, startColumn + 2).Value = HeaderOvertimeTierTwo;
        sheet.Cell(SubHeaderRow, startColumn + 3).Value = HeaderNightHours;
    }

    private static (string? AbsenceCode, decimal? Hours, bool IsWorkedDay, bool AbsenceSkipped) ResolveDayCell(
        List<PayrollDayEntry>? dayEntries,
        Dictionary<string, PayrollAbsenceMapping> absenceMapping)
    {
        if (dayEntries is null || dayEntries.Count == 0)
        {
            return (null, null, false, false);
        }

        var absenceEntry = dayEntries.FirstOrDefault(e => e.Kind == PayrollEntryKind.Absence);
        if (absenceEntry is not null)
        {
            var key = absenceEntry.AbsenceId?.ToString();
            if (key is not null
                && absenceMapping.TryGetValue(key, out var mapping)
                && !string.IsNullOrEmpty(mapping.Ausfallschluessel))
            {
                return (mapping.Ausfallschluessel, null, false, false);
            }

            var fallbackHours = SumHours(dayEntries);
            return (null, fallbackHours > 0 ? fallbackHours : null, fallbackHours > 0, true);
        }

        var hours = SumHours(dayEntries);
        return (null, hours > 0 ? hours : null, hours > 0, false);
    }

    private static decimal SumHours(List<PayrollDayEntry> dayEntries)
    {
        return dayEntries
            .Where(e => e.Kind is PayrollEntryKind.WorkHours or PayrollEntryKind.Surcharge)
            .Sum(e => e.Quantity);
    }

    private static List<DateOnly> GetPeriodDates(PayrollExportData data)
    {
        var dates = new List<DateOnly>();
        for (var date = data.StartDate; date <= data.EndDate; date = date.AddDays(1))
        {
            dates.Add(date);
        }

        return dates;
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
