// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Geo;

public sealed class CountryRegionEntry
{
    public string Name { get; set; } = string.Empty;

    public List<string> States { get; set; } = [];
}
