// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Geo;

public sealed class CountryRegionEntry
{
    public string Key { get; set; } = string.Empty;

    public Dictionary<string, string> Names { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> States { get; set; } = [];
}
