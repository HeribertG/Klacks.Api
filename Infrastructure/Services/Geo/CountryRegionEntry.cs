// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Geo;

public sealed class CountryRegionEntry
{
    public string Name { get; set; } = string.Empty;

    public List<string> States { get; set; } = [];
}
