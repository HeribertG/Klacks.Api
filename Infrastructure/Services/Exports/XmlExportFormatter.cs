// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as XML for ERP system integration.
/// Uses standard XML serialization with hierarchical structure.
/// </summary>
using System.Globalization;
using System.Text;
using System.Xml;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class XmlExportFormatter : IExportFormatter
{
    private const string SourceSystemIdElement = "SourceSystemId";

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

        if (!string.IsNullOrEmpty(order.SourceSystemId))
            writer.WriteElementString(SourceSystemIdElement, order.SourceSystemId);
        if (!string.IsNullOrEmpty(order.ExternalOrderReference))
            writer.WriteElementString(ErpImportXmlElements.ExternalOrderReference, order.ExternalOrderReference);

        if (order.OrderFromDate.HasValue)
            writer.WriteElementString("FromDate", order.OrderFromDate.Value.ToString("yyyy-MM-dd"));
        if (order.OrderUntilDate.HasValue)
            writer.WriteElementString("UntilDate", order.OrderUntilDate.Value.ToString("yyyy-MM-dd"));
        if (order.OrderStartShift.HasValue)
            writer.WriteElementString("StartShift", order.OrderStartShift.Value.ToString("HH:mm", CultureInfo.InvariantCulture));
        if (order.OrderEndShift.HasValue)
            writer.WriteElementString("EndShift", order.OrderEndShift.Value.ToString("HH:mm", CultureInfo.InvariantCulture));

        if (order.CustomerId.HasValue || !string.IsNullOrEmpty(order.CustomerName))
        {
            writer.WriteStartElement("Customer");
            if (order.CustomerId.HasValue)
                writer.WriteAttributeString("id", order.CustomerId.Value.ToString());
            if (order.CustomerNumber.HasValue)
                writer.WriteElementString("Number", order.CustomerNumber.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(order.CustomerName))
                writer.WriteElementString("Name", order.CustomerName);
            if (!string.IsNullOrEmpty(order.CustomerExternalReference))
                writer.WriteElementString(ErpImportXmlElements.ExternalCustomerReference, order.CustomerExternalReference);
            writer.WriteEndElement();
        }

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
        writer.WriteElementString("StartTime", work.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture));
        writer.WriteElementString("EndTime", work.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture));
        writer.WriteElementString("Hours", work.WorkTime.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteElementString("Surcharges", work.Surcharges.ToString("F2", CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(work.Information))
            writer.WriteElementString("Information", work.Information);

        ExportXmlElementWriter.WriteChanges(writer, work.Changes);
        ExportXmlElementWriter.WriteExpenses(writer, work.Expenses);
        ExportXmlElementWriter.WriteBreaks(writer, work.Breaks);

        writer.WriteEndElement();
    }
}
