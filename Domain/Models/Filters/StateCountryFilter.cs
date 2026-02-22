// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Filters;

public class StateCountryFilter
{
    public Guid Id { get; set; }

    public string Country { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public bool Select { get; set; }
}