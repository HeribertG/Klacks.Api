// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.Filter;

/// <summary>
/// One state picked in the shift list location filter: the state code and the country it belongs to. State codes are
/// only unique per country (BE is Bern in CH and Berlin in DE), so the pair travels together.
/// </summary>
public class ShiftFilterState
{
    public string State { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}
