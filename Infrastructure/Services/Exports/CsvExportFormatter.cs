// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as CSV with semicolon separator for ERP compatibility.
/// Flat structure: one row per work entry, work changes and expenses as additional rows.
/// </summary>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class CsvExportFormatter : IExportFormatter
{
    private const string Separator = ";";

    private static class EntryType
    {
        public const string Work = "Work";
        public const string WorkChange = "WorkChange";
        public const string Expenses = "Expenses";
        public const string Break = "Break";
    }

    public string FormatKey => ExportConstants.FormatCsv;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        var culture = CultureInfo.GetCultureInfo(options.Language);

        sb.AppendLine(string.Join(Separator,
            "OrderId", "OrderName", "OrderAbbreviation", "OrderFrom", "OrderUntil",
            "CustomerNumber", "CustomerName",
            "EntryType", "EmployeeId", "EmployeeName", "EmployeeIdNumber",
            "Date", "StartTime", "EndTime", "Hours", "Surcharges",
            "ChangeType", "Description", "ReplaceEmployee",
            "ExpenseAmount", "ExpenseTaxable",
            "AbsenceName", "Information"));

        foreach (var order in data.Orders)
        {
            var orderPrefix = BuildOrderPrefix(order, options);

            foreach (var work in order.WorkEntries)
            {
                AppendWorkRow(sb, orderPrefix, work, options, culture);

                foreach (var change in work.Changes)
                {
                    AppendChangeRow(sb, orderPrefix, work, change, options, culture);
                }

                foreach (var expense in work.Expenses)
                {
                    AppendExpenseRow(sb, orderPrefix, work, expense, options, culture);
                }

                foreach (var breakEntry in work.Breaks)
                {
                    AppendBreakRow(sb, orderPrefix, work, breakEntry, options, culture);
                }
            }
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string BuildOrderPrefix(OrderGroup order, ExportOptions options)
    {
        return string.Join(Separator,
            order.OrderShiftId,
            Escape(order.OrderName), Escape(order.OrderAbbreviation),
            FormatDate(order.OrderFromDate, options), FormatDate(order.OrderUntilDate, options),
            order.CustomerNumber?.ToString(CultureInfo.InvariantCulture) ?? "",
            Escape(order.CustomerName ?? ""));
    }

    private static void AppendWorkRow(StringBuilder sb, string orderPrefix, WorkExportEntry work, ExportOptions options, CultureInfo culture)
    {
        sb.AppendLine(string.Join(Separator,
            orderPrefix,
            EntryType.Work,
            work.EmployeeId, Escape(work.EmployeeName), work.EmployeeIdNumber,
            work.WorkDate.ToString(options.DateFormat, culture),
            work.StartTime.ToString(options.TimeFormat, culture),
            work.EndTime.ToString(options.TimeFormat, culture),
            work.WorkTime.ToString("F2", CultureInfo.InvariantCulture),
            work.Surcharges.ToString("F2", CultureInfo.InvariantCulture),
            "", Escape(work.Information ?? ""), "",
            "", "",
            "", ""));
    }

    private static void AppendChangeRow(StringBuilder sb, string orderPrefix, WorkExportEntry work, WorkChangeExportEntry change, ExportOptions options, CultureInfo culture)
    {
        sb.AppendLine(string.Join(Separator,
            orderPrefix,
            EntryType.WorkChange,
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

    private static void AppendExpenseRow(StringBuilder sb, string orderPrefix, WorkExportEntry work, ExpensesExportEntry expense, ExportOptions options, CultureInfo culture)
    {
        sb.AppendLine(string.Join(Separator,
            orderPrefix,
            EntryType.Expenses,
            work.EmployeeId, Escape(work.EmployeeName), work.EmployeeIdNumber,
            work.WorkDate.ToString(options.DateFormat, culture),
            "", "",
            "", "",
            "", Escape(expense.Description), "",
            expense.Amount.ToString("F2", CultureInfo.InvariantCulture), expense.Taxable ? "1" : "0",
            "", ""));
    }

    private static void AppendBreakRow(StringBuilder sb, string orderPrefix, WorkExportEntry work, BreakExportEntry breakEntry, ExportOptions options, CultureInfo culture)
    {
        sb.AppendLine(string.Join(Separator,
            orderPrefix,
            EntryType.Break,
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
