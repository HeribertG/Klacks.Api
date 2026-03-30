// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as CSV with semicolon separator for ERP compatibility.
/// Flat structure: one row per work entry, work changes and expenses as additional rows.
/// </summary>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class CsvExportFormatter : IExportFormatter
{
    public string FormatKey => ExportConstants.FormatCsv;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        var culture = CultureInfo.GetCultureInfo(options.Language);
        var separator = ";";

        sb.AppendLine(string.Join(separator,
            "OrderName", "OrderAbbreviation", "OrderFrom", "OrderUntil",
            "EntryType", "EmployeeId", "EmployeeName", "EmployeeIdNumber",
            "Date", "StartTime", "EndTime", "Hours", "Surcharges",
            "ChangeType", "Description", "ReplaceEmployee",
            "ExpenseAmount", "ExpenseTaxable",
            "AbsenceName", "Information"));

        foreach (var order in data.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                sb.AppendLine(string.Join(separator,
                    Escape(order.OrderName), Escape(order.OrderAbbreviation),
                    FormatDate(order.OrderFromDate, options), FormatDate(order.OrderUntilDate, options),
                    "Work",
                    work.EmployeeId, Escape(work.EmployeeName), work.EmployeeIdNumber,
                    work.WorkDate.ToString(options.DateFormat, culture),
                    work.StartTime.ToString(options.TimeFormat, culture),
                    work.EndTime.ToString(options.TimeFormat, culture),
                    work.WorkTime.ToString("F2", CultureInfo.InvariantCulture),
                    work.Surcharges.ToString("F2", CultureInfo.InvariantCulture),
                    "", Escape(work.Information ?? ""), "",
                    "", "",
                    "", ""));

                foreach (var change in work.Changes)
                {
                    sb.AppendLine(string.Join(separator,
                        Escape(order.OrderName), Escape(order.OrderAbbreviation),
                        FormatDate(order.OrderFromDate, options), FormatDate(order.OrderUntilDate, options),
                        "WorkChange",
                        work.EmployeeId, Escape(work.EmployeeName), work.EmployeeIdNumber,
                        work.WorkDate.ToString(options.DateFormat, culture),
                        change.StartTime.ToString(options.TimeFormat, culture),
                        change.EndTime.ToString(options.TimeFormat, culture),
                        change.ChangeTime.ToString("F2", CultureInfo.InvariantCulture),
                        change.Surcharges.ToString("F2", CultureInfo.InvariantCulture),
                        change.Type.ToString(), Escape(change.Description), Escape(change.ReplaceEmployeeName ?? ""),
                        "", change.ToInvoice ? "1" : "0",
                        "", ""));
                }

                foreach (var expense in work.Expenses)
                {
                    sb.AppendLine(string.Join(separator,
                        Escape(order.OrderName), Escape(order.OrderAbbreviation),
                        FormatDate(order.OrderFromDate, options), FormatDate(order.OrderUntilDate, options),
                        "Expenses",
                        work.EmployeeId, Escape(work.EmployeeName), work.EmployeeIdNumber,
                        work.WorkDate.ToString(options.DateFormat, culture),
                        "", "",
                        "", "",
                        "", Escape(expense.Description), "",
                        expense.Amount.ToString("F2", CultureInfo.InvariantCulture), expense.Taxable ? "1" : "0",
                        "", ""));
                }

                foreach (var breakEntry in work.Breaks)
                {
                    sb.AppendLine(string.Join(separator,
                        Escape(order.OrderName), Escape(order.OrderAbbreviation),
                        FormatDate(order.OrderFromDate, options), FormatDate(order.OrderUntilDate, options),
                        "Break",
                        work.EmployeeId, Escape(work.EmployeeName), work.EmployeeIdNumber,
                        breakEntry.BreakDate.ToString(options.DateFormat, culture),
                        breakEntry.StartTime.ToString(options.TimeFormat, culture),
                        breakEntry.EndTime.ToString(options.TimeFormat, culture),
                        breakEntry.BreakTime.ToString("F2", CultureInfo.InvariantCulture),
                        "", "",
                        "", "",
                        "", "",
                        Escape(breakEntry.AbsenceName), ""));
                }
            }
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string FormatDate(DateOnly? date, ExportOptions options)
    {
        return date?.ToString(options.DateFormat) ?? "";
    }
}
