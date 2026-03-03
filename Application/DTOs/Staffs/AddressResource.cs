// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Staffs;

public class AddressResource
{
    public string AddressLine1 { get; set; } = string.Empty;

    public string AddressLine2 { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public Guid ClientId { get; set; }

    public string Country { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string State { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Street2 { get; set; } = string.Empty;

    public string Street3 { get; set; } = string.Empty;

    public AddressTypeEnum Type { get; set; }

    public DateTime? ValidFrom { get; set; }

    public string Zip { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
