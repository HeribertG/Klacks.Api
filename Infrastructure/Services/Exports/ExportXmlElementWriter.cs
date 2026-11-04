// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared XmlWriter helpers for writing WorkChange/Expenses/Break elements. WriteChanges and WriteBreaks are
/// reused by the order-based and the client-period-based XML export formatters; WriteExpenses is used only by
/// the employee-based client-period formatter, because expenses never go into a customer (order) export.
/// </summary>
using System.Globalization;
using System.Xml;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public static class ExportXmlElementWriter
{
    private const string BreaksElement = "Breaks";
    private const string OnCallElement = "OnCall";
    private const string TrueValue = "true";

    public static void WriteChanges(XmlWriter writer, List<WorkChangeExportEntry> changes)
    {
        if (changes.Count > 0)
        {
            writer.WriteStartElement("Changes");
            foreach (var change in changes)
            {
                writer.WriteStartElement("Change");
                writer.WriteElementString("Type", change.Type.ToString());
                writer.WriteElementString("ChangeTime", change.ChangeTime.ToString("F2", CultureInfo.InvariantCulture));
                writer.WriteElementString("StartTime", change.StartTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture));
                writer.WriteElementString("EndTime", change.EndTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture));
                writer.WriteElementString("Description", XmlTextSanitizer.Clean(change.Description));
                writer.WriteElementString("Surcharges", change.Surcharges.ToString("F2", CultureInfo.InvariantCulture));
                writer.WriteElementString("ToInvoice", change.ToInvoice.ToString().ToLowerInvariant());
                if (!string.IsNullOrEmpty(change.ReplaceEmployeeName))
                    writer.WriteElementString("ReplaceEmployee", XmlTextSanitizer.Clean(change.ReplaceEmployeeName));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    public static void WriteExpenses(XmlWriter writer, List<ExpensesExportEntry> expenses)
    {
        if (expenses.Count > 0)
        {
            writer.WriteStartElement("Expenses");
            foreach (var expense in expenses)
            {
                writer.WriteStartElement("Expense");
                writer.WriteElementString("Amount", expense.Amount.ToString("F2", CultureInfo.InvariantCulture));
                writer.WriteElementString("Description", XmlTextSanitizer.Clean(expense.Description));
                writer.WriteElementString("Taxable", expense.Taxable.ToString().ToLowerInvariant());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    public static void WriteBreaks(XmlWriter writer, List<BreakExportEntry> breaks, string containerElement = BreaksElement)
    {
        if (breaks.Count > 0)
        {
            writer.WriteStartElement(containerElement);
            foreach (var breakEntry in breaks)
            {
                writer.WriteStartElement("Break");
                writer.WriteElementString("AbsenceName", XmlTextSanitizer.Clean(breakEntry.AbsenceName));
                writer.WriteElementString("Date", breakEntry.BreakDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture));
                writer.WriteElementString("StartTime", breakEntry.StartTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture));
                writer.WriteElementString("EndTime", breakEntry.EndTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture));
                writer.WriteElementString("Hours", breakEntry.BreakTime.ToString("F2", CultureInfo.InvariantCulture));
                if (breakEntry.IsOnCall)
                    writer.WriteElementString(OnCallElement, TrueValue);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}
