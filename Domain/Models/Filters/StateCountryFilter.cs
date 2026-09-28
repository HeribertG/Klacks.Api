// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Filters;

public class StateCountryFilter
{
    public Guid Id { get; set; }

    public string Country { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public bool Select { get; set; }
}