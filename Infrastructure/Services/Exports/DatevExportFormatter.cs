// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data in DATEV Buchungsstapel format (CSV with DATEV header).
/// Conforms to DATEV format specification for German tax consultants and accounting software.
/// Header row contains metadata (format version, data category, creation date).
/// Data rows contain booking entries mapped to DATEV field structure.
/// </summary>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class DatevExportFormatter : IExportFormatter
{
    private const string DatevFormatVersion = "700";
    private const int DataCategoryBookings = 21;
    private const string DatevDateFormat = "ddMM";
    private const string AccountLengthDefault = "4";

    public string FormatKey => ExportConstants.FormatDatev;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        var separator = ";";

        WriteHeaderRow(sb, data, options, separator);
        WriteColumnHeaders(sb, separator);
        WriteDataRows(sb, data, options, separator);

        return Encoding.GetEncoding(1252).GetBytes(sb.ToString());
    }

    private static void WriteHeaderRow(StringBuilder sb, OrderExportData data, ExportOptions options, string separator)
    {
        var fields = new List<string>
        {
            $"\"{DatevFormatVersion}\"",
            $"{DataCategoryBookings}",
            "\"Buchungsstapel\"",
            "1",
            $"{data.ExportDate:yyyyMMddHHmmss}000",
            "",
            "\"SV\"",
            "",
            "",
            "",
            $"\"{AccountLengthDefault}\"",
            $"{data.StartDate.Year}0101",
            $"{AccountLengthDefault}",
            $"{data.StartDate:yyyyMMdd}",
            $"{data.EndDate:yyyyMMdd}",
            "\"Klacks Export\"",
            "",
            "1",
            "0",
            "0",
            $"\"{options.CurrencyCode}\""
        };

        sb.AppendLine(string.Join(separator, fields));
    }

    private static void WriteColumnHeaders(StringBuilder sb, string separator)
    {
        var headers = new[]
        {
            "Umsatz (ohne Soll/Haben-Kz)", "Soll/Haben-Kennzeichen",
            "WKZ Umsatz", "Kurs", "Basis-Umsatz", "WKZ Basis-Umsatz",
            "Konto", "Gegenkonto (ohne BU-Schlüssel)", "BU-Schlüssel",
            "Belegdatum", "Belegfeld 1", "Belegfeld 2",
            "Skonto", "Buchungstext",
            "Postensperre", "Diverse Adressnummer", "Geschäftspartnerbank",
            "Sachverhalt", "Zinssperre",
            "Beleglink", "Beleginfo - Art 1", "Beleginfo - Inhalt 1",
            "Beleginfo - Art 2", "Beleginfo - Inhalt 2",
            "Beleginfo - Art 3", "Beleginfo - Inhalt 3",
            "Beleginfo - Art 4", "Beleginfo - Inhalt 4",
            "Beleginfo - Art 5", "Beleginfo - Inhalt 5",
            "Beleginfo - Art 6", "Beleginfo - Inhalt 6",
            "Beleginfo - Art 7", "Beleginfo - Inhalt 7",
            "Beleginfo - Art 8", "Beleginfo - Inhalt 8",
            "KOST1 - Kostenstelle", "KOST2 - Kostenstelle", "Kost-Menge",
            "EU-Land u. UStID", "EU-Steuersatz",
            "Abw. Versteuerungsart", "Sachverhalt L+L", "Funktionsergänzung L+L",
            "BU 49 Hauptfunktionstyp", "BU 49 Hauptfunktionsnummer",
            "BU 49 Funktionsergänzung", "Zusatzinformation - Art 1",
            "Zusatzinformation - Inhalt 1", "Zusatzinformation - Art 2",
            "Zusatzinformation - Inhalt 2", "Zusatzinformation - Art 3",
            "Zusatzinformation - Inhalt 3", "Zusatzinformation - Art 4",
            "Zusatzinformation - Inhalt 4", "Zusatzinformation - Art 5",
            "Zusatzinformation - Inhalt 5", "Zusatzinformation - Art 6",
            "Zusatzinformation - Inhalt 6", "Zusatzinformation - Art 7",
            "Zusatzinformation - Inhalt 7", "Zusatzinformation - Art 8",
            "Zusatzinformation - Inhalt 8", "Zusatzinformation - Art 9",
            "Zusatzinformation - Inhalt 9", "Zusatzinformation - Art 10",
            "Zusatzinformation - Inhalt 10", "Zusatzinformation - Art 11",
            "Zusatzinformation - Inhalt 11", "Zusatzinformation - Art 12",
            "Zusatzinformation - Inhalt 12", "Zusatzinformation - Art 13",
            "Zusatzinformation - Inhalt 13", "Zusatzinformation - Art 14",
            "Zusatzinformation - Inhalt 14", "Zusatzinformation - Art 15",
            "Zusatzinformation - Inhalt 15", "Zusatzinformation - Art 16",
            "Zusatzinformation - Inhalt 16", "Zusatzinformation - Art 17",
            "Zusatzinformation - Inhalt 17", "Zusatzinformation - Art 18",
            "Zusatzinformation - Inhalt 18", "Zusatzinformation - Art 19",
            "Zusatzinformation - Inhalt 19", "Zusatzinformation - Art 20",
            "Zusatzinformation - Inhalt 20",
            "Stück", "Gewicht",
            "Zahlweise", "Forderungsart", "Veranlagungsjahr", "Zugeordnete Fälligkeit",
            "Skontotyp", "Auftragsnummer", "Buchungstyp",
            "USt-Schlüssel (Anzahlungen)", "EU-Land (Anzahlungen)",
            "Sachverhalt L+L (Anzahlungen)", "EU-Steuersatz (Anzahlungen)",
            "Erlöskonto (Anzahlungen)", "Herkunft-Kz",
            "Buchungs GUID", "KOST-Datum", "SEPA-Mandatsreferenz",
            "Skontosperre", "Gesellschaftername", "Beteiligtennummer",
            "Identifikationsnummer", "Zeichnernummer",
            "Postensperre bis", "Bezeichnung SoBil-Sachverhalt",
            "Kennzeichen SoBil-Buchung", "Festschreibung",
            "Leistungsdatum", "Datum Zuord. Steuerperiode"
        };

        sb.AppendLine(string.Join(separator, headers));
    }

    private static void WriteDataRows(StringBuilder sb, OrderExportData data, ExportOptions options, string separator)
    {
        foreach (var order in data.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                WriteWorkBookingRow(sb, order, work, options, separator);

                foreach (var expense in work.Expenses)
                {
                    WriteExpenseBookingRow(sb, order, work, expense, options, separator);
                }
            }
        }
    }

    private static void WriteWorkBookingRow(
        StringBuilder sb, OrderGroup order, WorkExportEntry work, ExportOptions options, string separator)
    {
        var totalHours = work.WorkTime + work.Changes.Sum(c => c.ChangeTime);
        var amount = totalHours + work.Surcharges + work.Changes.Sum(c => c.Surcharges);

        var fields = new string[116];
        for (var i = 0; i < fields.Length; i++) fields[i] = "";

        fields[0] = FormatDecimal(amount);
        fields[1] = "S";
        fields[2] = $"\"{options.CurrencyCode}\"";
        fields[6] = $"\"{work.EmployeeIdNumber}\"";
        fields[7] = $"\"1600\"";
        fields[9] = work.WorkDate.ToString(DatevDateFormat);
        fields[10] = $"\"{order.OrderAbbreviation}\"";
        fields[13] = $"\"{EscapeDatev($"{work.EmployeeName} - {order.OrderName}")}\"";
        fields[36] = $"\"{EscapeDatev(order.OrderName)}\"";
        fields[38] = FormatDecimal(totalHours);
        fields[91] = $"\"{work.EmployeeIdNumber}\"";

        sb.AppendLine(string.Join(separator, fields));
    }

    private static void WriteExpenseBookingRow(
        StringBuilder sb, OrderGroup order, WorkExportEntry work, ExpensesExportEntry expense, ExportOptions options, string separator)
    {
        var fields = new string[116];
        for (var i = 0; i < fields.Length; i++) fields[i] = "";

        fields[0] = FormatDecimal(expense.Amount);
        fields[1] = "S";
        fields[2] = $"\"{options.CurrencyCode}\"";
        fields[6] = $"\"{work.EmployeeIdNumber}\"";
        fields[7] = expense.Taxable ? "\"4100\"" : "\"4900\"";
        fields[9] = work.WorkDate.ToString(DatevDateFormat);
        fields[10] = $"\"{order.OrderAbbreviation}\"";
        fields[13] = $"\"{EscapeDatev($"{expense.Description} - {work.EmployeeName}")}\"";
        fields[36] = $"\"{EscapeDatev(order.OrderName)}\"";

        sb.AppendLine(string.Join(separator, fields));
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("F2", CultureInfo.GetCultureInfo("de-DE")).Replace(".", "");
    }

    private static string EscapeDatev(string value)
    {
        return value.Replace("\"", "\"\"");
    }
}
