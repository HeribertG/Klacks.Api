// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Imports;

public class ImportedCustomerPayload
{
    public string SourceSystemId { get; set; } = string.Empty;

    public string ExternalCustomerReference { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Zip { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}
