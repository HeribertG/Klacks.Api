// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports client period data (hours/expenses/breaks per employee and external employee for a
/// date range) as XML, grouped by client instead of by order.
/// </summary>
using System.Globalization;
using System.Text;
using System.Xml;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ClientPeriodXmlExportFormatter : IClientPeriodExportFormatter
{
    public string FormatKey => ExportConstants.FormatXml;

    public string ContentType => ExportConstants.ContentTypeXml;

    public string FileExtension => ".xml";

    public byte[] Format(ClientPeriodExportData data, ExportOptions options)
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
            writer.WriteStartElement("ClientPeriodExport");

            writer.WriteAttributeString("startDate", data.StartDate.ToString("yyyy-MM-dd"));
            writer.WriteAttributeString("endDate", data.EndDate.ToString("yyyy-MM-dd"));
            writer.WriteAttributeString("exportDate", data.ExportDate.ToString("o"));
            writer.WriteAttributeString("currency", options.CurrencyCode);

            foreach (var client in data.Clients)
            {
                WriteClient(writer, client);
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return stream.ToArray();
    }

    private static void WriteClient(XmlWriter writer, ClientPeriodGroup client)
    {
        writer.WriteStartElement("Client");
        writer.WriteAttributeString("id", client.ClientId.ToString());
        writer.WriteElementString("IdNumber", client.ClientIdNumber.ToString(CultureInfo.InvariantCulture));
        writer.WriteElementString("Name", client.ClientName);
        writer.WriteElementString("Type", client.ClientType.ToString());

        writer.WriteStartElement("WorkEntries");

        foreach (var work in client.WorkEntries)
        {
            WriteWorkEntry(writer, work);
        }

        writer.WriteEndElement();

        WritePeriodHours(writer, client.PeriodHours);

        writer.WriteEndElement();
    }

    private static void WritePeriodHours(XmlWriter writer, List<ClientPeriodHoursExportEntry> periodHours)
    {
        if (periodHours.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("PeriodHours");

        foreach (var period in periodHours)
        {
            writer.WriteStartElement("Period");
            writer.WriteAttributeString("startDate", period.StartDate.ToString("yyyy-MM-dd"));
            writer.WriteAttributeString("endDate", period.EndDate.ToString("yyyy-MM-dd"));
            writer.WriteElementString("Hours", period.Hours.ToString("F2", CultureInfo.InvariantCulture));
            writer.WriteElementString("Surcharges", period.Surcharges.ToString("F2", CultureInfo.InvariantCulture));
            writer.WriteElementString("PaymentInterval", period.PaymentInterval);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteWorkEntry(XmlWriter writer, ClientWorkExportEntry work)
    {
        writer.WriteStartElement("Work");
        writer.WriteAttributeString("id", work.WorkId.ToString());

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
