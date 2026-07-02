// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Imports;

public class ImportedCustomerPayload
{
    public string SourceSystemId { get; set; } = string.Empty;

    public string ExternalCustomerReference { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Zip { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}
