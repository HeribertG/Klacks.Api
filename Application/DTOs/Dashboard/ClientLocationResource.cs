// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Dashboard;

public class ClientLocationResource
{
    public string Id { get; set; } = string.Empty;
    public int Type { get; set; }
    public AddressInfo? CurrentAddress { get; set; }
}

public class AddressInfo
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
