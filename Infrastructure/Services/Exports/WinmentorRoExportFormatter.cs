// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats employee-centric payroll data as a WinMENTOR (Romania) "Import pontaje" workbook,
/// structura 2 (daily timesheet): sheet "pontaj", one row per employee spanning the whole export
/// period, with An/Luna/Simbol formatie/Identificator formatie/Marca identity columns, one column
/// per calendar day of the period, and two four-column summary blocks (AVANS / LICHIDARE) each with
/// "zile lucrate", "ore suplim.I", "ore supllim.II" and "ore noapte" sub-columns, followed by an
/// "OBSERVATII:" column. Data rows start on row 3, matching the vendor sample's own instruction
/// ("linia 3 coloana 1"). The day grid is always the fixed 31 day-of-month columns of the vendor
/// sample (columns F..AJ), so the AVANS/LICHIDARE/OBSERVATII columns sit at fixed positions
/// (AK, AO, AS) whatever the month length; days a month does not have stay blank.
/// </summary>
/// <remarks>
/// Layout source (recorded by an earlier review, not re-checked in the latest audit — see below):
/// the public WinMENTOR sample workbook (downloaded and parsed cell-by-cell,
/// not just described in docs): http://download.winmentor.ro/WinMentor/Documentatie/03_SALARII/Solutii/Import%20pontaje%20structura%202%20exemplu.xls
/// Sheet name, Windows-1252 codepage, "linia 3 coloana 1" data start, the 5 identity columns, the
/// 31 day-of-month columns and the AVANS/LICHIDARE 4-field blocks (incl. the vendor's own
/// "supllim.II" and "Identi ficator formatie" spelling, preserved verbatim here for fidelity) were
/// all confirmed this way. This corrects docs/knowledge/export-samples/findings-ch-pl-hu-gr-ro-balkan-ie-uk.md
/// section 5 (RO), whose prose summary omitted the day-of-month columns entirely.
/// Specification documents for this structure ("Structura import pontaje zilnice 2_xls - martie 2021"):
/// http://ftp.winmentor.ro/WinMentor/Documentatie/03_SALARII/Solutii/Structura%20import%20pontaje%20zilnice%202_xls%20-%20martie%202021.pdf
/// Audit 2026-11-02: none of the winmentor.ro hosts (download/ftp/portal) were reachable from the audit
/// environment, so the sample and the PDFs could NOT be re-read. The one deviation fixed then follows
/// from the sample findings recorded above: the sample has a fixed 31-column day grid, while this
/// formatter had emitted one column per period date, which shifted the AVANS/LICHIDARE/OBSERVATII
/// columns left in months shorter than 31 days (e.g. February: AVANS at AH instead of AK).
/// Everything else in this header (header texts, merges, numeric hour cells, code cells) is unchanged
/// and still rests only on the earlier review.
/// Absence codes: the sister specification for structure 1 ("Structura import pontaje zilnice 1_xls - martie 2021",
/// https://download2.winmentor.ro/WinMentor/Documentatie/03_SALARII/2022/Structura%20import%20pontaje%20zilnice%201_xls%20-%20martie%202021.pdf,
/// read 2026-11-02) types every day field as Numeric (max. 2 digits) holding either that day's hours or the
/// numeric code of an interruption from its legend (codes 25..57, e.g. 33 = Co concediu odihnă, 57 = Tm telemuncă).
/// AbsenceMappingJson.Ausfallschluessel is the tenant-supplied WinMENTOR code: when it parses as a plain
/// non-negative integer (e.g. "33") it is written as a number cell, matching the numeric day field; any other
/// value (e.g. a legacy symbol like "CO") is written as text unchanged, for backward compatibility with
/// existing mappings. Tenants should map to the numeric legend codes.
/// Known simplifications, all deliberate MVP scope cuts:
/// (1) File is written as .xlsx via ClosedXML, which cannot write the legacy .xls (OLE2/BIFF) format. The
/// structure-1 specification states the import file type as ".xls (Excel)"; whether a given WinMENTOR version
/// also accepts .xlsx is not confirmed. If the import rejects the file, the customer has to open it in Excel and
/// re-save it as "Excel 97-2003 Workbook (*.xls)" before importing.
/// (2) "Simbol formatie" / "Identi ficator formatie" (department code) are left blank: Klacks'
/// PayrollExportGroupConfig has no per-employee department-code field to source them from.
/// (3) A day cell holds worked hours only. Surcharge quantities are premium time (a surcharge on hours
/// already worked), not additional worked hours, so adding them to the day cell would overstate the worked
/// time; they are not written and are counted in SkippedUnmappedSurchargeCount (whatever their unit). The
/// AVANS/LICHIDARE "ore suplim.I" / "ore supllim.II" / "ore noapte" sub-columns are always left blank because
/// PayrollDayEntry has no overtime-tier or night-hours distinction to source them from.
/// (4) Klacks has no salary-advance information, so the AVANS block is left empty and the worked days of the
/// whole month are written to LICHIDARE, following the vendor sample's OBSERVATII rule ("without an advance
/// leave AVANS empty, month total in LICHIDARE").
/// (5) A day with a mapped Absence entry writes the absence code instead of hours; an unmapped
/// Absence is counted in SkippedAbsenceCount (never dropped silently) and the day cell falls back
/// to that day's summed WorkHours quantity instead. The hours and further absences of a day whose cell
/// holds an absence code (e.g. on-call duty plus a call-out) cannot be written and are counted in
/// SkippedSupersededCount.
/// (6) RecordCount here counts emitted employee rows (one row = the whole period), not individual
/// entries, because this format is a per-employee calendar matrix rather than one row per entry.
/// (7) The import is per month (An/Luna, derived from StartDate). Entries dated outside StartDate's
/// calendar month cannot be placed in the fixed day-of-month grid without overwriting that month's
/// cells; they are not written and are counted in SkippedSupersededCount (the closest existing
/// counter), so they are never dropped silently.
/// </remarks>
using System.Globalization;
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
    private const int DayColumnCount = 31;
    private const int SummaryBlockColumnCount = 4;
    private const int HeaderRow = 1;
    private const int SubHeaderRow = 2;
    private const int FirstDataRow = 3;

    public string FormatKey => PayrollExportConstants.FormatKeyWinmentorRo;

    public string ContentType => ExportConstants.ContentTypeXlsx;

    public string FileExtension => ".xlsx";

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PayrollAbsenceMapping>(config.AbsenceMappingJson, counter);
        var dates = GetMonthDates(data);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        var avansStartColumn = IdentityColumnCount + DayColumnCount + 1;
        var lichidareStartColumn = avansStartColumn + SummaryBlockColumnCount;
        var observationsColumn = lichidareStartColumn + SummaryBlockColumnCount;

        WriteHeader(sheet, avansStartColumn, lichidareStartColumn, observationsColumn);

        var row = FirstDataRow;
        var recordCount = 0;

        foreach (var employee in data.Employees)
        {
            var entriesByDate = employee.Entries.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => g.ToList());
            counter.Superseded += employee.Entries.Count(e => !IsInExportMonth(e.Date, data.StartDate));

            sheet.Cell(row, 1).Value = data.StartDate.Year;
            sheet.Cell(row, 2).Value = data.StartDate.Month;
            sheet.Cell(row, IdentityColumnCount).Value = employee.IdNumber;

            var workedDays = 0;

            foreach (var date in dates)
            {
                var column = DayColumn(date.Day);
                entriesByDate.TryGetValue(date, out var dayEntries);

                var (absenceCode, hours, isWorkedDay) = ResolveDayCell(dayEntries, absenceMapping, counter);

                if (absenceCode is not null)
                {
                    WriteAbsenceCode(sheet.Cell(row, column), absenceCode);
                }
                else if (hours.HasValue)
                {
                    sheet.Cell(row, column).Value = hours.Value;
                }

                if (isWorkedDay)
                {
                    workedDays++;
                }
            }

            sheet.Cell(row, lichidareStartColumn).Value = workedDays;

            recordCount++;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return counter.ToResult(stream.ToArray(), recordCount);
    }

    private static void WriteHeader(
        IXLWorksheet sheet,
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

        for (var day = 1; day <= DayColumnCount; day++)
        {
            var column = DayColumn(day);
            sheet.Cell(HeaderRow, column).Value = day;
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

    /// <summary>
    /// Writes a mapped absence code: a plain non-negative integer (the numeric legend codes of the WinMENTOR
    /// day fields, e.g. 33) as a number cell, anything else as text.
    /// </summary>
    private static void WriteAbsenceCode(IXLCell cell, string absenceCode)
    {
        cell.Value = int.TryParse(absenceCode, NumberStyles.None, CultureInfo.InvariantCulture, out var numericCode)
            ? numericCode
            : absenceCode;
    }

    /// <summary>
    /// Decides the one cell of a day and accounts for every entry of that day: a mapped absence code wins the cell
    /// and supersedes all other entries of the day (they are counted, not dropped); without one, the WorkHours
    /// entries are summed into the cell. Surcharge entries are never written (premium time, not worked hours) and
    /// are counted as unmapped surcharges. Unmapped absences, day-based hour entries and unknown kinds are counted.
    /// </summary>
    private static (string? AbsenceCode, decimal? Hours, bool IsWorkedDay) ResolveDayCell(
        List<PayrollDayEntry>? dayEntries,
        Dictionary<string, PayrollAbsenceMapping> absenceMapping,
        PayrollExportSkipCounter counter)
    {
        if (dayEntries is null || dayEntries.Count == 0)
        {
            return (null, null, false);
        }

        PayrollDayEntry? codeEntry = null;
        string? absenceCode = null;
        var hourEntries = new List<PayrollDayEntry>();
        var mappedAbsences = new List<PayrollDayEntry>();

        foreach (var entry in dayEntries)
        {
            switch (entry.Kind)
            {
                case PayrollEntryKind.Surcharge:
                    counter.UnmappedSurcharges++;
                    break;

                case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
                    counter.UnsupportedUnits++;
                    break;

                case PayrollEntryKind.WorkHours:
                    hourEntries.Add(entry);
                    break;

                case PayrollEntryKind.Absence:
                    var key = entry.AbsenceId?.ToString();
                    if (key is null
                        || !absenceMapping.TryGetValue(key, out var mapping)
                        || string.IsNullOrEmpty(mapping.Ausfallschluessel))
                    {
                        counter.UnmappedAbsences++;
                        break;
                    }

                    if (codeEntry is null)
                    {
                        codeEntry = entry;
                        absenceCode = mapping.Ausfallschluessel;
                    }
                    else
                    {
                        mappedAbsences.Add(entry);
                    }

                    break;

                default:
                    counter.UnsupportedKinds++;
                    break;
            }
        }

        if (codeEntry is not null)
        {
            counter.Emitted++;
            counter.Superseded += hourEntries.Count + mappedAbsences.Count;
            return (absenceCode, null, false);
        }

        counter.Emitted += hourEntries.Count;
        var hours = hourEntries.Sum(e => e.Quantity);
        return (null, hours > 0 ? hours : null, hours > 0);
    }

    private static int DayColumn(int dayOfMonth) => IdentityColumnCount + dayOfMonth;

    private static bool IsInExportMonth(DateOnly date, DateOnly start) =>
        date.Year == start.Year && date.Month == start.Month;

    /// <summary>
    /// The period dates that fall into StartDate's calendar month; the fixed day grid can hold only one month.
    /// </summary>
    private static List<DateOnly> GetMonthDates(PayrollExportData data)
    {
        var dates = new List<DateOnly>();
        for (var date = data.StartDate; date <= data.EndDate && IsInExportMonth(date, data.StartDate); date = date.AddDays(1))
        {
            dates.Add(date);
        }

        return dates;
    }
}
