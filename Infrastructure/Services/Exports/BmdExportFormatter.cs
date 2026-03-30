// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data in BMD NTCS format for Austrian accounting systems.
/// Generates CSV with BMD-specific field structure for booking import.
/// Fields: Satzart, Buchungsdatum, Belegnummer, Kontonummer, Gegenkonto, Betrag, Text, Kostenstelle.
/// </summary>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class BmdExportFormatter : IExportFormatter
{
    private const string BmdSatzartBuchung = "0";

    public string FormatKey => ExportConstants.FormatBmd;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        var separator = ";";

        sb.AppendLine(string.Join(separator,
            "satzart", "konto", "gkonto", "belession", "buchdat",
            "bession", "buchsymbol", "betrag", "steession",
            "text", "kost", "mession"));

        var bookingNumber = 1;

        foreach (var order in data.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                var totalAmount = work.WorkTime + work.Surcharges
                    + work.Changes.Sum(c => c.ChangeTime + c.Surcharges);

                sb.AppendLine(string.Join(separator,
                    BmdSatzartBuchung,
                    work.EmployeeIdNumber.ToString(),
                    "3600",
                    bookingNumber.ToString(),
                    work.WorkDate.ToString("dd.MM.yyyy"),
                    order.OrderAbbreviation,
                    "ER",
                    FormatAmount(totalAmount),
                    "",
                    EscapeBmd($"{work.EmployeeName} - {order.OrderName}"),
                    EscapeBmd(order.OrderName),
                    work.WorkTime.ToString("F2", CultureInfo.InvariantCulture)));

                bookingNumber++;

                foreach (var expense in work.Expenses)
                {
                    sb.AppendLine(string.Join(separator,
                        BmdSatzartBuchung,
                        work.EmployeeIdNumber.ToString(),
                        expense.Taxable ? "7600" : "7800",
                        bookingNumber.ToString(),
                        work.WorkDate.ToString("dd.MM.yyyy"),
                        order.OrderAbbreviation,
                        "ER",
                        FormatAmount(expense.Amount),
                        "",
                        EscapeBmd($"{expense.Description} - {work.EmployeeName}"),
                        EscapeBmd(order.OrderName),
                        ""));

                    bookingNumber++;
                }
            }
        }

        return Encoding.GetEncoding(1252).GetBytes(sb.ToString());
    }

    private static string FormatAmount(decimal value)
    {
        return value.ToString("F2", CultureInfo.GetCultureInfo("de-AT"));
    }

    private static string EscapeBmd(string value)
    {
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
