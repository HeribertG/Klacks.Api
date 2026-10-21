// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public class AddressSuggestion
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
