// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as ZUGFeRD/XRechnung XML conforming to EN 16931.
/// Generates Cross-Industry Invoice (CII) XML for EU-compliant e-invoicing.
/// Each order group becomes a separate invoice line.
/// </summary>
using System.Globalization;
using System.Text;
using System.Xml;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ZugferdExportFormatter : IExportFormatter
{
    private const string CiiNamespace = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    private const string RsmNamespace = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";
    private const string QdtNamespace = "urn:un:unece:uncefact:data:standard:QualifiedDataType:100";
    private const string UdtNamespace = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100";

    public string FormatKey => ExportConstants.FormatZugferd;

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
            writer.WriteStartElement("rsm", "CrossIndustryInvoice", CiiNamespace);
            writer.WriteAttributeString("xmlns", "ram", null, RsmNamespace);
            writer.WriteAttributeString("xmlns", "qdt", null, QdtNamespace);
            writer.WriteAttributeString("xmlns", "udt", null, UdtNamespace);

            WriteExchangedDocumentContext(writer);
            WriteExchangedDocument(writer, data);
            WriteSupplyChainTradeTransaction(writer, data, options);

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return stream.ToArray();
    }

    private static void WriteExchangedDocumentContext(XmlWriter writer)
    {
        writer.WriteStartElement("rsm", "ExchangedDocumentContext", CiiNamespace);

        writer.WriteStartElement("ram", "GuidelineSpecifiedDocumentContextParameter", RsmNamespace);
        writer.WriteStartElement("ram", "ID", RsmNamespace);
        writer.WriteString("urn:cen.eu:en16931:2017");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private static void WriteExchangedDocument(XmlWriter writer, OrderExportData data)
    {
        writer.WriteStartElement("rsm", "ExchangedDocument", CiiNamespace);

        writer.WriteStartElement("ram", "ID", RsmNamespace);
        writer.WriteString($"KLACKS-{data.ExportDate:yyyyMMddHHmmss}");
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "TypeCode", RsmNamespace);
        writer.WriteString("380");
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "IssueDateTime", RsmNamespace);
        writer.WriteStartElement("udt", "DateTimeString", UdtNamespace);
        writer.WriteAttributeString("format", "102");
        writer.WriteString(data.ExportDate.ToString("yyyyMMdd"));
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private static void WriteSupplyChainTradeTransaction(XmlWriter writer, OrderExportData data, ExportOptions options)
    {
        writer.WriteStartElement("rsm", "SupplyChainTradeTransaction", CiiNamespace);

        var lineNumber = 1;
        foreach (var order in data.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                WriteInvoiceLine(writer, order, work, lineNumber, options);
                lineNumber++;
            }
        }

        WriteTradeAgreement(writer, options);
        WriteTradeDelivery(writer, data);
        WriteTradeSettlement(writer, data, options);

        writer.WriteEndElement();
    }

    private static void WriteInvoiceLine(XmlWriter writer, OrderGroup order, WorkExportEntry work, int lineNumber, ExportOptions options)
    {
        writer.WriteStartElement("ram", "IncludedSupplyChainTradeLineItem", RsmNamespace);

        writer.WriteStartElement("ram", "AssociatedDocumentLineDocument", RsmNamespace);
        writer.WriteStartElement("ram", "LineID", RsmNamespace);
        writer.WriteString(lineNumber.ToString());
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "SpecifiedTradeProduct", RsmNamespace);
        writer.WriteStartElement("ram", "Name", RsmNamespace);
        writer.WriteString($"{order.OrderName} - {work.EmployeeName} ({work.WorkDate:yyyy-MM-dd})");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "SpecifiedLineTradeAgreement", RsmNamespace);
        writer.WriteStartElement("ram", "NetPriceProductTradePrice", RsmNamespace);
        WriteAmount(writer, work.WorkTime + work.Surcharges, options.CurrencyCode);
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "SpecifiedLineTradeDelivery", RsmNamespace);
        writer.WriteStartElement("ram", "BilledQuantity", RsmNamespace);
        writer.WriteAttributeString("unitCode", "HUR");
        writer.WriteString(work.WorkTime.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "SpecifiedLineTradeSettlement", RsmNamespace);
        writer.WriteStartElement("ram", "SpecifiedTradeSettlementLineMonetarySummation", RsmNamespace);
        WriteLineTotal(writer, work.WorkTime + work.Surcharges, options.CurrencyCode);
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private static void WriteTradeAgreement(XmlWriter writer, ExportOptions options)
    {
        var company = options.Company;
        var sellerName = !string.IsNullOrEmpty(company.Name) ? company.Name : "Klacks";

        writer.WriteStartElement("ram", "ApplicableHeaderTradeAgreement", RsmNamespace);

        writer.WriteStartElement("ram", "SellerTradeParty", RsmNamespace);
        writer.WriteStartElement("ram", "Name", RsmNamespace);
        writer.WriteString(sellerName);
        writer.WriteEndElement();

        if (!string.IsNullOrEmpty(company.TaxId))
        {
            writer.WriteStartElement("ram", "SpecifiedTaxRegistration", RsmNamespace);
            writer.WriteStartElement("ram", "ID", RsmNamespace);
            writer.WriteAttributeString("schemeID", "FC");
            writer.WriteString(company.TaxId);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        if (!string.IsNullOrEmpty(company.VatId))
        {
            writer.WriteStartElement("ram", "SpecifiedTaxRegistration", RsmNamespace);
            writer.WriteStartElement("ram", "ID", RsmNamespace);
            writer.WriteAttributeString("schemeID", "VA");
            writer.WriteString(company.VatId);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        writer.WriteStartElement("ram", "BuyerTradeParty", RsmNamespace);
        writer.WriteStartElement("ram", "Name", RsmNamespace);
        writer.WriteString("Customer");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private static void WriteTradeDelivery(XmlWriter writer, OrderExportData data)
    {
        writer.WriteStartElement("ram", "ApplicableHeaderTradeDelivery", RsmNamespace);

        writer.WriteStartElement("ram", "ActualDeliverySupplyChainEvent", RsmNamespace);
        writer.WriteStartElement("ram", "OccurrenceDateTime", RsmNamespace);
        writer.WriteStartElement("udt", "DateTimeString", UdtNamespace);
        writer.WriteAttributeString("format", "102");
        writer.WriteString(data.EndDate.ToString("yyyyMMdd"));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private static void WriteTradeSettlement(XmlWriter writer, OrderExportData data, ExportOptions options)
    {
        writer.WriteStartElement("ram", "ApplicableHeaderTradeSettlement", RsmNamespace);

        writer.WriteStartElement("ram", "InvoiceCurrencyCode", RsmNamespace);
        writer.WriteString(options.CurrencyCode);
        writer.WriteEndElement();

        var grandTotal = data.Orders
            .SelectMany(o => o.WorkEntries)
            .Sum(w => w.WorkTime + w.Surcharges + w.Expenses.Sum(e => e.Amount));

        writer.WriteStartElement("ram", "SpecifiedTradeSettlementHeaderMonetarySummation", RsmNamespace);

        writer.WriteStartElement("ram", "LineTotalAmount", RsmNamespace);
        writer.WriteString(grandTotal.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "TaxBasisTotalAmount", RsmNamespace);
        writer.WriteString(grandTotal.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "GrandTotalAmount", RsmNamespace);
        writer.WriteString(grandTotal.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteEndElement();

        writer.WriteStartElement("ram", "DuePayableAmount", RsmNamespace);
        writer.WriteString(grandTotal.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteEndElement();

        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private static void WriteAmount(XmlWriter writer, decimal amount, string currency)
    {
        writer.WriteStartElement("ram", "ChargeAmount", RsmNamespace);
        writer.WriteString(amount.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }

    private static void WriteLineTotal(XmlWriter writer, decimal amount, string currency)
    {
        writer.WriteStartElement("ram", "LineTotalAmount", RsmNamespace);
        writer.WriteString(amount.ToString("F2", CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }
}
