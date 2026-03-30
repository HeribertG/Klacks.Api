// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as XML for ERP system integration.
/// Uses standard XML serialization with hierarchical structure.
/// </summary>
using System.Globalization;
using System.Text;
using System.Xml;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class XmlExportFormatter : IExportFormatter
{
    public string FormatKey => ExportConstants.FormatXml;

    public string ContentType => ExportConstants.ContentTypeXml;

    public string FileExtension => ".xml";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false)
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("OrderExport");

            writer.WriteAttributeString("startDate", data.StartDate.ToString("yyyy-MM-dd"));
            writer.WriteAttributeString("endDate", data.EndDate.ToString("yyyy-MM-dd"));
            writer.WriteAttributeString("exportDate", data.ExportDate.ToString("o"));
            writer.WriteAttributeString("currency", options.CurrencyCode);

            foreach (var order in data.Orders)
            {
                WriteOrder(writer, order, options);
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return stream.ToArray();
    }

    private static void WriteOrder(XmlWriter writer, OrderGroup order, ExportOptions options)
    {
        writer.WriteStartElement("Order");
        writer.WriteAttributeString("id", order.OrderShiftId.ToString());
        writer.WriteElementString("Name", order.OrderName);
        writer.WriteElementString("Abbreviation", order.OrderAbbreviation);

        if (order.OrderFromDate.HasValue)
            writer.WriteElementString("FromDate", order.OrderFromDate.Value.ToString("yyyy-MM-dd"));
        if (order.OrderUntilDate.HasValue)
            writer.WriteElementString("UntilDate", order.OrderUntilDate.Value.ToString("yyyy-MM-dd"));
        if (order.OrderStartShift.HasValue)
            writer.WriteElementString("StartShift", order.OrderStartShift.Value.ToString("HH:mm"));
        if (order.OrderEndShift.HasValue)
            writer.WriteElementString("EndShift", order.OrderEndShift.Value.ToString("HH:mm"));

        writer.WriteStartElement("WorkEntries");

        foreach (var work in order.WorkEntries)
        {
            WriteWorkEntry(writer, work);
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteWorkEntry(XmlWriter writer, WorkExportEntry work)
    {
        writer.WriteStartElement("Work");
        writer.WriteAttributeString("id", work.WorkId.ToString());

        writer.WriteElementString("EmployeeId", work.EmployeeId.ToString());
        writer.WriteElementString("EmployeeName", work.EmployeeName);
        writer.WriteElementString("EmployeeIdNumber", work.EmployeeIdNumber.ToString());
        writer.WriteElementString("Date", work.WorkDate.ToString("yyyy-MM-dd"));
        writer.WriteElementString("StartTime", work.StartTime.ToString("HH:mm"));
        writer.WriteElementString("EndTime", work.EndTime.ToString("HH:mm"));
        writer.WriteElementString("Hours", work.WorkTime.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteElementString("Surcharges", work.Surcharges.ToString("F2", CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(work.Information))
            writer.WriteElementString("Information", work.Information);

        if (work.Changes.Count > 0)
        {
            writer.WriteStartElement("Changes");
            foreach (var change in work.Changes)
            {
                writer.WriteStartElement("Change");
                writer.WriteElementString("Type", change.Type.ToString());
                writer.WriteElementString("ChangeTime", change.ChangeTime.ToString("F2", CultureInfo.InvariantCulture));
                writer.WriteElementString("StartTime", change.StartTime.ToString("HH:mm"));
                writer.WriteElementString("EndTime", change.EndTime.ToString("HH:mm"));
                writer.WriteElementString("Description", change.Description);
                writer.WriteElementString("Surcharges", change.Surcharges.ToString("F2", CultureInfo.InvariantCulture));
                writer.WriteElementString("ToInvoice", change.ToInvoice.ToString().ToLowerInvariant());
                if (!string.IsNullOrEmpty(change.ReplaceEmployeeName))
                    writer.WriteElementString("ReplaceEmployee", change.ReplaceEmployeeName);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        if (work.Expenses.Count > 0)
        {
            writer.WriteStartElement("Expenses");
            foreach (var expense in work.Expenses)
            {
                writer.WriteStartElement("Expense");
                writer.WriteElementString("Amount", expense.Amount.ToString("F2", CultureInfo.InvariantCulture));
                writer.WriteElementString("Description", expense.Description);
                writer.WriteElementString("Taxable", expense.Taxable.ToString().ToLowerInvariant());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        if (work.Breaks.Count > 0)
        {
            writer.WriteStartElement("Breaks");
            foreach (var breakEntry in work.Breaks)
            {
                writer.WriteStartElement("Break");
                writer.WriteElementString("AbsenceName", breakEntry.AbsenceName);
                writer.WriteElementString("Date", breakEntry.BreakDate.ToString("yyyy-MM-dd"));
                writer.WriteElementString("StartTime", breakEntry.StartTime.ToString("HH:mm"));
                writer.WriteElementString("EndTime", breakEntry.EndTime.ToString("HH:mm"));
                writer.WriteElementString("Hours", breakEntry.BreakTime.ToString("F2", CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }
}
