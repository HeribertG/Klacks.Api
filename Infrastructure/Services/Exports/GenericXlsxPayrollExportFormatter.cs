// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Country-agnostic Excel payroll export ("Tier A" in the country-pack research): the same four
/// columns as GenericDelimitedPayrollExportFormatter (PersonnelNumber, Date, WageType, Quantity) as
/// a single-sheet .xlsx workbook, for target systems whose import wizard expects an Excel template
/// rather than a delimited text file (ES a3nom Conceptos Variables, PT Cegid Primavera, GR Epsilon
/// Net, HU Kulcs-Ber). One group config selects this formatter; no per-country formatter class needed.
/// </summary>
/// <remarks>
/// Config.Delimiter and Config.Encoding are ignored (Excel has no delimiter/text-encoding concept);
/// only Config.BaseWageType, Config.SurchargeWageType and Config.AbsenceMappingJson apply, exactly as
/// in the CSV counterpart. Dates are written as real Excel dates (not text) so the target's import
/// wizard can apply its own locale-specific date parsing/formatting.
/// </remarks>
using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class GenericXlsxPayrollExportFormatter : IPayrollExportFormatter
{
    private const string SheetName = "Payroll";
    private const string HeaderPersonnelNumber = "PersonnelNumber";
    private const string HeaderDate = "Date";
    private const string HeaderWageType = "WageType";
    private const string HeaderQuantity = "Quantity";

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyGenericPayrollXlsx;

    public string ContentType => ExportConstants.ContentTypeXlsx;

    public string FileExtension => ".xlsx";

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var absenceMapping = ParseAbsenceMapping(config.AbsenceMappingJson);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        sheet.Cell(1, 1).Value = HeaderPersonnelNumber;
        sheet.Cell(1, 2).Value = HeaderDate;
        sheet.Cell(1, 3).Value = HeaderWageType;
        sheet.Cell(1, 4).Value = HeaderQuantity;

        var row = 2;
        var recordCount = 0;
        var skippedAbsenceCount = 0;

        foreach (var employee in data.Employees)
        {
            foreach (var entry in employee.Entries)
            {
                string wageType;

                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours:
                        wageType = config.BaseWageType;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (string.IsNullOrEmpty(config.SurchargeWageType))
                        {
                            continue;
                        }

                        wageType = config.SurchargeWageType;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                        {
                            skippedAbsenceCount++;
                            continue;
                        }

                        wageType = mapping.WageType;
                        break;

                    default:
                        continue;
                }

                sheet.Cell(row, 1).Value = employee.IdNumber;
                sheet.Cell(row, 2).Value = entry.Date.ToDateTime(TimeOnly.MinValue);
                sheet.Cell(row, 2).Style.DateFormat.Format = "yyyy-mm-dd";
                sheet.Cell(row, 3).Value = wageType;
                sheet.Cell(row, 4).Value = entry.Quantity;
                row++;
                recordCount++;
            }
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
