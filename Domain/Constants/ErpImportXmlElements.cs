// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Element, attribute and constant names for the Klacks-defined ERP order import XML contract.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpImportXmlElements
{
    public const int CurrentSchemaVersion = 1;

    public const string Root = "ErpOrderImport";
    public const string SchemaVersionAttribute = "schemaVersion";
    public const string SourceSystemIdAttribute = "sourceSystemId";

    public const string Order = "Order";
    public const string ExternalOrderReference = "ExternalOrderReference";
    public const string FromDate = "FromDate";
    public const string UntilDate = "UntilDate";
    public const string StartTime = "StartTime";
    public const string EndTime = "EndTime";
    public const string IsTimeRange = "IsTimeRange";
    public const string Quantity = "Quantity";
    public const string SumEmployees = "SumEmployees";

    public const string Weekdays = "Weekdays";
    public const string Monday = "monday";
    public const string Tuesday = "tuesday";
    public const string Wednesday = "wednesday";
    public const string Thursday = "thursday";
    public const string Friday = "friday";
    public const string Saturday = "saturday";
    public const string Sunday = "sunday";

    public const string Customer = "Customer";
    public const string ExternalCustomerReference = "ExternalCustomerReference";
    public const string Company = "Company";
    public const string Address = "Address";
    public const string Street = "Street";
    public const string Zip = "Zip";
    public const string City = "City";
    public const string Country = "Country";
}
