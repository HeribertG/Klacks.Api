// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Dashboard;

public class AddressInfo
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
