// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Exports order data as XML for ERP system integration.
/// Uses standard XML serialization with hierarchical structure (Order -> Work -> Changes/Breaks). Expenses and
/// allowances belong to the employee, not the customer, and are therefore not part of the order export.
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
            NewLineChars = ExportConstants.LineEnding,
            Encoding = new UTF8Encoding(false)
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("OrderExport");

            writer.WriteAttributeString("startDate", data.StartDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture));
            writer.WriteAttributeString("endDate", data.EndDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture));
            writer.WriteAttributeString("exportDate", data.ExportDate.ToString("o"));
            writer.WriteAttributeString("currency", XmlTextSanitizer.Clean(options.CurrencyCode));

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
        writer.WriteElementString("Name", XmlTextSanitizer.Clean(order.OrderName));
        writer.WriteElementString("Abbreviation", XmlTextSanitizer.Clean(order.OrderAbbreviation));

        if (!string.IsNullOrEmpty(order.SourceSystemId))
            writer.WriteElementString(SourceSystemIdElement, XmlTextSanitizer.Clean(order.SourceSystemId));
        if (!string.IsNullOrEmpty(order.ExternalOrderReference))
            writer.WriteElementString(ErpImportXmlElements.ExternalOrderReference, XmlTextSanitizer.Clean(order.ExternalOrderReference));

        if (order.OrderFromDate.HasValue)
            writer.WriteElementString("FromDate", order.OrderFromDate.Value.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture));
        if (order.OrderUntilDate.HasValue)
            writer.WriteElementString("UntilDate", order.OrderUntilDate.Value.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture));
        if (order.OrderStartShift.HasValue)
            writer.WriteElementString("StartShift", order.OrderStartShift.Value.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture));
        if (order.OrderEndShift.HasValue)
            writer.WriteElementString("EndShift", order.OrderEndShift.Value.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture));

        if (order.CustomerId.HasValue || !string.IsNullOrEmpty(order.CustomerName))
        {
            writer.WriteStartElement("Customer");
            if (order.CustomerId.HasValue)
                writer.WriteAttributeString("id", order.CustomerId.Value.ToString());
            if (order.CustomerNumber.HasValue)
                writer.WriteElementString("Number", order.CustomerNumber.Value.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(order.CustomerName))
                writer.WriteElementString("Name", XmlTextSanitizer.Clean(order.CustomerName));
            if (!string.IsNullOrEmpty(order.CustomerExternalReference))
                writer.WriteElementString(ErpImportXmlElements.ExternalCustomerReference, XmlTextSanitizer.Clean(order.CustomerExternalReference));
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
        writer.WriteElementString("EmployeeName", XmlTextSanitizer.Clean(work.EmployeeName));
        writer.WriteElementString("EmployeeIdNumber", work.EmployeeIdNumber.ToString());
        writer.WriteElementString("Date", work.WorkDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture));
        writer.WriteElementString("StartTime", work.StartTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture));
        writer.WriteElementString("EndTime", work.EndTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture));
        writer.WriteElementString("Hours", work.WorkTime.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteElementString("Surcharges", work.Surcharges.ToString("F2", CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(work.Information))
            writer.WriteElementString("Information", XmlTextSanitizer.Clean(work.Information));

        ExportXmlElementWriter.WriteChanges(writer, work.Changes);
        ExportXmlElementWriter.WriteBreaks(writer, work.Breaks);

        writer.WriteEndElement();
    }
}
