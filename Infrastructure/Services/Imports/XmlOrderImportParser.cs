// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses the Klacks-defined ERP order import XML contract into neutral payload DTOs.
/// Invalid or incomplete orders (missing required fields, no weekday, until-date before
/// from-date, duplicate reference within the same file) are rejected and reported
/// individually so one bad row does not block a batch.
/// </summary>
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Imports;

namespace Klacks.Api.Infrastructure.Services.Imports;

public class XmlOrderImportParser : IOrderImportParser
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";
    private const int MinutesPerHour = 60;

    public OrderImportParseResult Parse(Stream xmlStream)
    {
        var result = new OrderImportParseResult();

        XDocument document;
        try
        {
            document = XDocument.Load(xmlStream, LoadOptions.SetLineInfo);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or InvalidOperationException)
        {
            result.Errors.Add(new OrderImportValidationError { Field = ErpImportXmlElements.Root, Message = $"Malformed XML: {ex.Message}" });
            return result;
        }

        var root = document.Root;
        if (root is null || root.Name.LocalName != ErpImportXmlElements.Root)
        {
            result.Errors.Add(new OrderImportValidationError { Field = ErpImportXmlElements.Root, Message = $"Root element must be <{ErpImportXmlElements.Root}>." });
            return result;
        }

        var schemaVersionText = root.Attribute(ErpImportXmlElements.SchemaVersionAttribute)?.Value;
        if (!int.TryParse(schemaVersionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var schemaVersion))
        {
            result.Errors.Add(new OrderImportValidationError { LineNumber = LineOf(root), Field = ErpImportXmlElements.SchemaVersionAttribute, Message = "Missing or non-numeric schemaVersion attribute." });
            return result;
        }

        result.SchemaVersion = schemaVersion;
        if (schemaVersion != ErpImportXmlElements.CurrentSchemaVersion)
        {
            result.Errors.Add(new OrderImportValidationError { LineNumber = LineOf(root), Field = ErpImportXmlElements.SchemaVersionAttribute, Message = $"Unsupported schemaVersion {schemaVersion}; expected {ErpImportXmlElements.CurrentSchemaVersion}." });
            return result;
        }

        var sourceSystemId = root.Attribute(ErpImportXmlElements.SourceSystemIdAttribute)?.Value;
        if (string.IsNullOrWhiteSpace(sourceSystemId))
        {
            result.Errors.Add(new OrderImportValidationError { LineNumber = LineOf(root), Field = ErpImportXmlElements.SourceSystemIdAttribute, Message = "Missing sourceSystemId attribute." });
            return result;
        }

        var seenReferences = new HashSet<string>(StringComparer.Ordinal);
        foreach (var orderElement in root.Elements(ErpImportXmlElements.Order))
        {
            var errorsBefore = result.Errors.Count;
            var order = ParseOrder(orderElement, sourceSystemId, result.Errors);
            var isValid = result.Errors.Count == errorsBefore;

            if (isValid && !seenReferences.Add(order.ExternalOrderReference))
            {
                result.Errors.Add(new OrderImportValidationError
                {
                    LineNumber = LineOf(orderElement),
                    Field = ErpImportXmlElements.ExternalOrderReference,
                    Message = $"Duplicate <{ErpImportXmlElements.ExternalOrderReference}> '{order.ExternalOrderReference}' within the same file; the duplicate order is rejected."
                });
                isValid = false;
            }

            if (isValid)
            {
                result.Orders.Add(order);
            }
            else
            {
                StampOrderReference(result.Errors, errorsBefore, order.ExternalOrderReference);
            }
        }

        return result;
    }

    private static ImportedOrderPayload ParseOrder(XElement orderElement, string sourceSystemId, List<OrderImportValidationError> errors)
    {
        var order = new ImportedOrderPayload { SourceSystemId = sourceSystemId };

        order.ExternalOrderReference = RequireText(orderElement, ErpImportXmlElements.ExternalOrderReference, errors);
        order.Description = OptionalText(orderElement, ErpImportXmlElements.Description);
        order.Customer = ParseCustomer(orderElement, sourceSystemId, errors);
        order.FromDate = RequireDate(orderElement, ErpImportXmlElements.FromDate, errors);
        order.UntilDate = OptionalDate(orderElement, ErpImportXmlElements.UntilDate, errors);
        order.StartTime = RequireTime(orderElement, ErpImportXmlElements.StartTime, errors);
        order.EndTime = RequireTime(orderElement, ErpImportXmlElements.EndTime, errors);
        order.IsTimeRange = OptionalBool(orderElement, ErpImportXmlElements.IsTimeRange, false);
        order.DurationMinutes = ParseDurationTotalMinutes(orderElement, errors);
        if (order.IsTimeRange && order.DurationMinutes is null)
        {
            errors.Add(new OrderImportValidationError { LineNumber = LineOf(orderElement), Field = ErpImportXmlElements.Duration, Message = $"<{ErpImportXmlElements.Duration}> is required when <{ErpImportXmlElements.IsTimeRange}> is true." });
        }

        order.Quantity = OptionalInt(orderElement, ErpImportXmlElements.Quantity, 1);
        order.SumEmployees = OptionalInt(orderElement, ErpImportXmlElements.SumEmployees, 1);

        var weekdays = orderElement.Element(ErpImportXmlElements.Weekdays);
        order.IsMonday = WeekdayFlag(weekdays, ErpImportXmlElements.Monday);
        order.IsTuesday = WeekdayFlag(weekdays, ErpImportXmlElements.Tuesday);
        order.IsWednesday = WeekdayFlag(weekdays, ErpImportXmlElements.Wednesday);
        order.IsThursday = WeekdayFlag(weekdays, ErpImportXmlElements.Thursday);
        order.IsFriday = WeekdayFlag(weekdays, ErpImportXmlElements.Friday);
        order.IsSaturday = WeekdayFlag(weekdays, ErpImportXmlElements.Saturday);
        order.IsSunday = WeekdayFlag(weekdays, ErpImportXmlElements.Sunday);

        if (!HasAnyWeekday(order))
        {
            errors.Add(new OrderImportValidationError { LineNumber = LineOf(orderElement), Field = ErpImportXmlElements.Weekdays, Message = $"At least one weekday must be true in <{ErpImportXmlElements.Weekdays}>; incomplete orders are rejected." });
        }

        if (order.UntilDate is { } untilDate && order.FromDate != default && untilDate < order.FromDate)
        {
            errors.Add(new OrderImportValidationError { LineNumber = LineOf(orderElement), Field = ErpImportXmlElements.UntilDate, Message = $"<{ErpImportXmlElements.UntilDate}> must not be before <{ErpImportXmlElements.FromDate}>." });
        }

        return order;
    }

    private static bool HasAnyWeekday(ImportedOrderPayload order)
    {
        return order.IsMonday || order.IsTuesday || order.IsWednesday || order.IsThursday
            || order.IsFriday || order.IsSaturday || order.IsSunday;
    }

    private static void StampOrderReference(List<OrderImportValidationError> errors, int firstIndex, string externalOrderReference)
    {
        if (string.IsNullOrWhiteSpace(externalOrderReference))
        {
            return;
        }

        for (var i = firstIndex; i < errors.Count; i++)
        {
            errors[i].ExternalOrderReference = externalOrderReference;
        }
    }

    private static ImportedCustomerPayload ParseCustomer(XElement orderElement, string sourceSystemId, List<OrderImportValidationError> errors)
    {
        var customer = new ImportedCustomerPayload { SourceSystemId = sourceSystemId };
        var customerElement = orderElement.Element(ErpImportXmlElements.Customer);
        if (customerElement is null)
        {
            errors.Add(new OrderImportValidationError { LineNumber = LineOf(orderElement), Field = ErpImportXmlElements.Customer, Message = $"Missing <{ErpImportXmlElements.Customer}> element." });
            return customer;
        }

        customer.ExternalCustomerReference = (customerElement.Element(ErpImportXmlElements.ExternalCustomerReference)?.Value ?? string.Empty).Trim();
        customer.Company = RequireText(customerElement, ErpImportXmlElements.Company, errors);

        var addressElement = customerElement.Element(ErpImportXmlElements.Address);
        if (addressElement is null)
        {
            errors.Add(new OrderImportValidationError { LineNumber = LineOf(customerElement), Field = ErpImportXmlElements.Address, Message = $"Missing <{ErpImportXmlElements.Address}> element." });
            return customer;
        }

        customer.Street = RequireText(addressElement, ErpImportXmlElements.Street, errors);
        customer.Zip = RequireText(addressElement, ErpImportXmlElements.Zip, errors);
        customer.City = OptionalText(addressElement, ErpImportXmlElements.City);
        customer.State = OptionalText(addressElement, ErpImportXmlElements.State);
        customer.Country = OptionalText(addressElement, ErpImportXmlElements.Country);

        return customer;
    }

    private static string RequireText(XElement parent, string elementName, List<OrderImportValidationError> errors)
    {
        var value = parent.Element(elementName)?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new OrderImportValidationError { LineNumber = LineOf(parent), Field = elementName, Message = $"Missing or empty <{elementName}>." });
            return string.Empty;
        }

        return value;
    }

    private static DateOnly RequireDate(XElement parent, string elementName, List<OrderImportValidationError> errors)
    {
        var text = parent.Element(elementName)?.Value?.Trim();
        if (DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            return value;
        }

        errors.Add(new OrderImportValidationError { LineNumber = LineOf(parent), Field = elementName, Message = $"Missing or invalid <{elementName}> (expected {DateFormat})." });
        return default;
    }

    private static DateOnly? OptionalDate(XElement parent, string elementName, List<OrderImportValidationError> errors)
    {
        var text = parent.Element(elementName)?.Value?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            return value;
        }

        errors.Add(new OrderImportValidationError { LineNumber = LineOf(parent), Field = elementName, Message = $"Invalid <{elementName}> (expected {DateFormat})." });
        return null;
    }

    private static TimeOnly RequireTime(XElement parent, string elementName, List<OrderImportValidationError> errors)
    {
        var text = parent.Element(elementName)?.Value?.Trim();
        if (TimeOnly.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            return value;
        }

        errors.Add(new OrderImportValidationError { LineNumber = LineOf(parent), Field = elementName, Message = $"Missing or invalid <{elementName}> (expected {TimeFormat})." });
        return default;
    }

    private static string OptionalText(XElement parent, string elementName)
    {
        return (parent.Element(elementName)?.Value ?? string.Empty).Trim();
    }

    private static int? ParseDurationTotalMinutes(XElement orderElement, List<OrderImportValidationError> errors)
    {
        var durationElement = orderElement.Element(ErpImportXmlElements.Duration);
        if (durationElement is null)
        {
            return null;
        }

        var errorsBefore = errors.Count;
        var hours = OptionalNonNegativeInt(durationElement, ErpImportXmlElements.DurationHours, ErpImportXmlElements.Duration, errors);
        var minutes = OptionalNonNegativeInt(durationElement, ErpImportXmlElements.DurationMinutes, ErpImportXmlElements.Duration, errors);
        if (errors.Count > errorsBefore)
        {
            return null;
        }

        var totalMinutes = hours * MinutesPerHour + minutes;
        if (totalMinutes == 0)
        {
            errors.Add(new OrderImportValidationError { LineNumber = LineOf(durationElement), Field = ErpImportXmlElements.Duration, Message = $"<{ErpImportXmlElements.Duration}> must be greater than zero minutes in total." });
            return null;
        }

        return totalMinutes;
    }

    private static int OptionalNonNegativeInt(XElement parent, string elementName, string errorField, List<OrderImportValidationError> errors)
    {
        var text = parent.Element(elementName)?.Value?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0)
        {
            return value;
        }

        errors.Add(new OrderImportValidationError { LineNumber = LineOf(parent), Field = errorField, Message = $"Invalid <{elementName}> (expected a non-negative integer)." });
        return 0;
    }

    private static bool OptionalBool(XElement parent, string elementName, bool defaultValue)
    {
        var text = parent.Element(elementName)?.Value?.Trim();
        return bool.TryParse(text, out var value) ? value : defaultValue;
    }

    private static int OptionalInt(XElement parent, string elementName, int defaultValue)
    {
        var text = parent.Element(elementName)?.Value?.Trim();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
    }

    private static bool WeekdayFlag(XElement? weekdays, string attributeName)
    {
        var text = weekdays?.Attribute(attributeName)?.Value;
        return bool.TryParse(text, out var value) && value;
    }

    private static int? LineOf(XElement element)
    {
        var lineInfo = (IXmlLineInfo)element;
        return lineInfo.HasLineInfo() ? lineInfo.LineNumber : null;
    }
}
