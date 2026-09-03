// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the demo order definitions as an ERP order import document following the Klacks import
/// contract, distributing the orders over the seeded customers so the import reuses them instead of
/// creating duplicates. Every order carries an explicit duration because the seeded work time is not
/// always the clock distance between start and end. Orders start on the first day of the current
/// month rather than on the fixed base date of the SQL seed, so a fresh install does not receive a
/// plan that begins in the past.
/// </summary>
/// <param name="timeProvider">Clock the order start date is derived from</param>

using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Klacks.Api.Application.DTOs.Seed;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Data.Seed.Demo;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Persistence.Seed.Demo;

public class DemoOrderXmlGenerator : IDemoOrderXmlGenerator
{
    private readonly TimeProvider _timeProvider;

    public DemoOrderXmlGenerator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";
    private const int MinutesPerHour = 60;
    private const string TrueText = "true";
    private const string FalseText = "false";
    private const string XmlIndentation = "  ";

    public string Generate(IReadOnlyList<DemoOrderCustomer> customers, string language)
    {
        ArgumentNullException.ThrowIfNull(customers);
        if (customers.Count == 0)
        {
            throw new ArgumentException("At least one customer is required to assign demo orders to.", nameof(customers));
        }

        var registry = new DemoSeedNameRegistry(language);
        var factory = new DemoOrderDefinitionFactory(language, registry, ResolveOrderStartDate());

        var definitions = factory.CreateShiftOrders()
            .Concat(factory.CreateTimeRangeOrders())
            .ToList();

        var root = new XElement(
            ErpImportXmlElements.Root,
            new XAttribute(ErpImportXmlElements.SchemaVersionAttribute, ErpImportXmlElements.CurrentSchemaVersion),
            new XAttribute(ErpImportXmlElements.SourceSystemIdAttribute, DemoOrderSeedConstants.SourceSystemId));

        for (var i = 0; i < definitions.Count; i++)
        {
            var customer = customers[i % customers.Count];
            root.Add(BuildOrder(definitions[i], i + 1, customer));
        }

        return Render(new XDocument(root));
    }

    private static XElement BuildOrder(DemoOrderDefinition definition, int sequenceNumber, DemoOrderCustomer customer)
    {
        var order = new XElement(
            ErpImportXmlElements.Order,
            new XElement(ErpImportXmlElements.ExternalOrderReference, BuildOrderReference(sequenceNumber)),
            new XElement(ErpImportXmlElements.Description, definition.Description),
            new XElement(
                ErpImportXmlElements.Duration,
                new XElement(ErpImportXmlElements.DurationHours, definition.WorkTimeMinutes / MinutesPerHour),
                new XElement(ErpImportXmlElements.DurationMinutes, definition.WorkTimeMinutes % MinutesPerHour)),
            new XElement(ErpImportXmlElements.FromDate, definition.FromDate.ToString(DateFormat, CultureInfo.InvariantCulture)));

        if (definition.UntilDate is { } untilDate)
        {
            order.Add(new XElement(ErpImportXmlElements.UntilDate, untilDate.ToString(DateFormat, CultureInfo.InvariantCulture)));
        }

        order.Add(
            new XElement(ErpImportXmlElements.StartTime, definition.StartShift.ToString(TimeFormat, CultureInfo.InvariantCulture)),
            new XElement(ErpImportXmlElements.EndTime, definition.EndShift.ToString(TimeFormat, CultureInfo.InvariantCulture)),
            new XElement(ErpImportXmlElements.IsTimeRange, Text(definition.IsTimeRange)),
            new XElement(ErpImportXmlElements.Quantity, definition.Quantity),
            new XElement(ErpImportXmlElements.SumEmployees, definition.SumEmployees),
            new XElement(
                ErpImportXmlElements.Weekdays,
                new XAttribute(ErpImportXmlElements.Monday, Text(definition.IsMonday)),
                new XAttribute(ErpImportXmlElements.Tuesday, Text(definition.IsTuesday)),
                new XAttribute(ErpImportXmlElements.Wednesday, Text(definition.IsWednesday)),
                new XAttribute(ErpImportXmlElements.Thursday, Text(definition.IsThursday)),
                new XAttribute(ErpImportXmlElements.Friday, Text(definition.IsFriday)),
                new XAttribute(ErpImportXmlElements.Saturday, Text(definition.IsSaturday)),
                new XAttribute(ErpImportXmlElements.Sunday, Text(definition.IsSunday))),
            BuildCustomer(customer));

        return order;
    }

    private static XElement BuildCustomer(DemoOrderCustomer customer)
    {
        return new XElement(
            ErpImportXmlElements.Customer,
            new XElement(ErpImportXmlElements.ExternalCustomerReference, BuildCustomerReference(customer)),
            new XElement(ErpImportXmlElements.Company, customer.Company),
            new XElement(
                ErpImportXmlElements.Address,
                new XElement(ErpImportXmlElements.Street, customer.Street),
                new XElement(ErpImportXmlElements.Zip, customer.Zip),
                new XElement(ErpImportXmlElements.City, customer.City),
                new XElement(ErpImportXmlElements.State, customer.State),
                new XElement(ErpImportXmlElements.Country, customer.Country)));
    }

    public static string BuildOrderReference(int sequenceNumber)
    {
        return DemoOrderSeedConstants.OrderReferencePrefix
            + sequenceNumber.ToString(DemoOrderSeedConstants.OrderReferenceNumberFormat, CultureInfo.InvariantCulture);
    }

    public static string BuildCustomerReference(DemoOrderCustomer customer)
    {
        return DemoOrderSeedConstants.CustomerReferencePrefix
            + customer.IdNumber.ToString(DemoOrderSeedConstants.CustomerReferenceNumberFormat, CultureInfo.InvariantCulture);
    }

    private static string Text(bool value)
    {
        return value ? TrueText : FalseText;
    }

    private DateOnly ResolveOrderStartDate()
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return new DateOnly(today.Year, today.Month, DemoOrderSeedConstants.FirstDayOfMonth);
    }

    private static string Render(XDocument document)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = XmlIndentation,
            Encoding = encoding
        };

        using var buffer = new MemoryStream();
        using (var writer = XmlWriter.Create(buffer, settings))
        {
            document.Save(writer);
        }

        return encoding.GetString(buffer.ToArray());
    }
}
