// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Exports payroll data as a Logo (Turkey) "Excel'den Puantaj Bilgileri Aktarımı" sheet (payroll family, key
/// "logo-bordro-tr"): a single-sheet .xlsx with the three-row structure of Logo's puantaj import — row 1 free
/// description labels ("açıklama"), row 2 the five-digit reference codes ("referans satırı") that map each column
/// to a Logo field, row 3+ one row per employee ("Üçüncü satırdan itibaren de siciller listelenecektir").
/// Identity columns Sicil No / Ad / Soyad are followed by one column per distinct configured puantaj reference code,
/// whose cells hold that employee's aggregated hours (Saat code) or days (Gün code) for that record.
/// </summary>
/// <remarks>
/// Spec source: Logo destek dokümanı "Excel'den Puantaj Bilgileri Aktarımı" (İnsan Kaynakları / Gold Bordro,
/// Doc No IK-0040 / GB-0026, Rev. 2, 15.06.2005),
/// https://docs.logo.com.tr/download/attachments/50679972/Support%20Excelden%20Puantaj%20Bilgileri%20Aktar%C4%B1m%C4%B1.pdf?api=v2
/// Reference codes are five digits XYZTV:
/// - X: 0 = mesai/hak (puantaj) data, 9 = sicil/firma data.
/// - Y (X=0): 1 Mesai, 2 Ek Mesai, 3 Sosyal Yardımlar, 4 Ek Ödemeler, 5 Ek Kesintiler. Y (X=9): 1 Firma.
/// - ZT: 01-99, the record number of the mesai/hak line in the tenant's Logo definitions.
/// - V: XY=00 → 1 Sicil No; X=0 → 2 Gün, 3 Saat, 4 Tutar; X=9 → 2 Ad, 3 Soyad; XY=91 → 1 Firma No.
/// Accordingly Sicil No is "00001"; Ad and Soyad use X=9 with V=2/3 and Y=0 (the sicil block, since Y=1 is Firma),
/// i.e. "90002"/"90003" — the document gives the X and V digits, the Y=0/ZT=00 fill is inferred from the scheme.
/// Klacks does not know a tenant's ZT record numbers, so wage-type columns take their code verbatim from the group
/// config (BaseWageType, SurchargeWageType, each absence mapping's WageType) and the customer copies it from row 2 of
/// the blank template Logo exports ("boş/format aktarım"). A configured code is only used when it is a valid puantaj
/// code (0, Y 1-5, ZT 01-99, V 2 or 3); otherwise the entry is counted as unmapped. Its V digit fixes the unit: an
/// hour-based entry needs a Saat code (V=3), a day-based entry (on-call duty) a Gün code (V=2); a unit mismatch is
/// not written and counted in SkippedUnsupportedUnitCount. Tutar codes (V=4) are never filled, Klacks exports no
/// amounts. Columns are ordered by reference code, mirroring the template order Mesai → Ek Kesinti. Quantities are
/// written as real Excel numbers. The optional Firma No column (91001) is not emitted: Logo's own blank export
/// already scopes the sheet "firma bazında" and Klacks holds no Logo firm number. The sheet name is not prescribed
/// by the document; "Puantaj" is used. Ad and Soyad are written from the structured PayrollEmployee.FirstName and
/// PayrollEmployee.LastName.
/// </remarks>
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
    private const string HeaderSicilNo = "Sicil Numarası";
    private const string HeaderAd = "Ad";
    private const string HeaderSoyad = "Soyad";
    private const string SicilNoCode = "00001";
    private const string AdCode = "90002";
    private const string SoyadCode = "90003";
    private const string EmptyCode = "";
    private const int ReferenceCodeLength = 5;
    private const char PuantajBlockDigit = '0';
    private const char GunDigit = '2';
    private const char SaatDigit = '3';
    private const string HeaderGun = "Gün";
    private const string HeaderSaat = "Saat";

    private static readonly string[] PuantajGroupLabels =
    [
        "Mesai",
        "Ek Mesai",
        "Sosyal Yardım",
        "Ek Ödeme",
        "Ek Kesinti",
    ];

    private const int SicilNoColumn = 1;
    private const int AdColumn = 2;
    private const int SoyadColumn = 3;
    private const int FirstDataColumn = 4;

    public string FormatKey => PayrollExportConstants.FormatKeyLogoBordroTr;

    public string ContentType => ExportConstants.ContentTypeXlsx;

    public string FileExtension => ".xlsx";

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PayrollAbsenceMapping>(config.AbsenceMappingJson, counter);
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

        foreach (var employee in data.Employees)
        {
            var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);

            foreach (var entry in employee.Entries)
            {
                var code = ResolveWageCode(entry, config, absenceMapping, counter);
                if (string.IsNullOrEmpty(code))
                {
                    continue;
                }

                if (!UnitMatchesCode(entry.Unit, code))
                {
                    counter.UnsupportedUnits++;
                    continue;
                }

                counter.Emitted++;
                totals.TryGetValue(code, out var current);
                totals[code] = current + entry.Quantity;
            }

            WriteEmployeeRow(sheet, row, employee, totals, wageColumnIndex);
            row++;
            recordCount++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return counter.ToResult(stream.ToArray(), recordCount);
    }

    private static void WriteHeaderAndCodeRows(IXLWorksheet sheet, IReadOnlyList<string> wageCodes)
    {
        sheet.Cell(HeaderRowNumber, SicilNoColumn).Value = HeaderSicilNo;
        sheet.Cell(HeaderRowNumber, AdColumn).Value = HeaderAd;
        sheet.Cell(HeaderRowNumber, SoyadColumn).Value = HeaderSoyad;

        WriteCode(sheet, SicilNoColumn, SicilNoCode);
        WriteCode(sheet, AdColumn, AdCode);
        WriteCode(sheet, SoyadColumn, SoyadCode);

        for (var i = 0; i < wageCodes.Count; i++)
        {
            var column = FirstDataColumn + i;
            sheet.Cell(HeaderRowNumber, column).Value = DescribeCode(wageCodes[i]);
            WriteCode(sheet, column, wageCodes[i]);
        }
    }

    private static void WriteCode(IXLWorksheet sheet, int column, string code)
    {
        var cell = sheet.Cell(CodeRowNumber, column);
        cell.Style.NumberFormat.Format = "@";
        cell.Value = code;
    }

    private static string DescribeCode(string code)
    {
        var group = PuantajGroupLabels[code[1] - '1'];
        var unit = code[4] == GunDigit ? HeaderGun : HeaderSaat;
        return $"{group} {code.Substring(2, 2)} {unit}";
    }

    private static void WriteEmployeeRow(
        IXLWorksheet sheet,
        int row,
        PayrollEmployee employee,
        Dictionary<string, decimal> totals,
        Dictionary<string, int> wageColumnIndex)
    {
        sheet.Cell(row, SicilNoColumn).Value = employee.IdNumber;
        sheet.Cell(row, AdColumn).Value = employee.FirstName;
        sheet.Cell(row, SoyadColumn).Value = employee.LastName;

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
        PayrollExportSkipCounter counter)
    {
        switch (entry.Kind)
        {
            case PayrollEntryKind.WorkHours:
                if (!IsPuantajCode(config.BaseWageType))
                {
                    counter.UnmappedBaseWages++;
                    return EmptyCode;
                }

                return config.BaseWageType;

            case PayrollEntryKind.Surcharge:
                if (!IsPuantajCode(config.SurchargeWageType))
                {
                    counter.UnmappedSurcharges++;
                    return EmptyCode;
                }

                return config.SurchargeWageType;

            case PayrollEntryKind.Absence:
                var key = entry.AbsenceId?.ToString();
                if (key == null || !absenceMapping.TryGetValue(key, out var mapping) || !IsPuantajCode(mapping.WageType))
                {
                    counter.UnmappedAbsences++;
                    return EmptyCode;
                }

                return mapping.WageType;

            default:
                counter.UnsupportedKinds++;
                return EmptyCode;
        }
    }

    private static bool UnitMatchesCode(PayrollQuantityUnit unit, string code)
    {
        var expected = unit == PayrollQuantityUnit.Days ? GunDigit : SaatDigit;
        return code[4] == expected;
    }

    private static bool IsPuantajCode(string? code)
    {
        if (code == null || code.Length != ReferenceCodeLength || !code.All(char.IsAsciiDigit))
        {
            return false;
        }

        var group = code[1];
        var recordNumber = code.Substring(2, 2);
        var field = code[4];

        return code[0] == PuantajBlockDigit
            && group >= '1' && group <= '5'
            && recordNumber != "00"
            && (field == GunDigit || field == SaatDigit);
    }

    private static List<string> BuildWageCodeColumns(
        PayrollExportGroupConfig config,
        Dictionary<string, PayrollAbsenceMapping> absenceMapping)
    {
        var codes = new SortedSet<string>(StringComparer.Ordinal);

        void Add(string? code)
        {
            if (IsPuantajCode(code))
            {
                codes.Add(code!);
            }
        }

        Add(config.BaseWageType);
        Add(config.SurchargeWageType);
        foreach (var mapping in absenceMapping.Values)
        {
            Add(mapping.WageType);
        }

        return codes.ToList();
    }
}
