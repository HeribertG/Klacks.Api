// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared XmlWriter helpers for writing WorkChange/Expenses/Break elements, reused by both
/// the order-based and client-period-based XML export formatters.
/// </summary>
using System.Globalization;
using System.Xml;
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
                writer.WriteElementString("StartTime", change.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture));
                writer.WriteElementString("EndTime", change.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture));
                writer.WriteElementString("Description", change.Description);
                writer.WriteElementString("Surcharges", change.Surcharges.ToString("F2", CultureInfo.InvariantCulture));
                writer.WriteElementString("ToInvoice", change.ToInvoice.ToString().ToLowerInvariant());
                if (!string.IsNullOrEmpty(change.ReplaceEmployeeName))
                    writer.WriteElementString("ReplaceEmployee", change.ReplaceEmployeeName);
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
                writer.WriteElementString("Description", expense.Description);
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
                writer.WriteElementString("AbsenceName", breakEntry.AbsenceName);
                writer.WriteElementString("Date", breakEntry.BreakDate.ToString("yyyy-MM-dd"));
                writer.WriteElementString("StartTime", breakEntry.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture));
                writer.WriteElementString("EndTime", breakEntry.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture));
                writer.WriteElementString("Hours", breakEntry.BreakTime.ToString("F2", CultureInfo.InvariantCulture));
                if (breakEntry.IsOnCall)
                    writer.WriteElementString(OnCallElement, TrueValue);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}
