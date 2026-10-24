// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.DTOs.Filter;

public class ShiftFilter : BaseFilter
{
    public bool ActiveDateRange { get; set; }

    public bool FormerDateRange { get; set; }

    public bool FutureDateRange { get; set; }

    public string SearchString { get; set; } = string.Empty;

    public ShiftFilterType FilterType { get; set; } = ShiftFilterType.Original;

    public bool IncludeClientName { get; set; }

    public bool IsSealedOrder { get; set; }

    public bool IsTimeRange { get; set; }

    public bool IsSporadic { get; set; }

    public Guid? SelectedGroup { get; set; }

    /// <summary>
    /// Optional order location filters on the customer address (workplace, else main address; never invoicing):
    /// country and state by code or name in any installed language, city by case-insensitive equality, zip by prefix.
    /// </summary>
    public string? Country { get; set; }

    public string? State { get; set; }

    public string? City { get; set; }

    public string? ZipPrefix { get; set; }

    public bool HasLocationFilter =>
        !string.IsNullOrWhiteSpace(Country)
        || !string.IsNullOrWhiteSpace(State)
        || !string.IsNullOrWhiteSpace(City)
        || !string.IsNullOrWhiteSpace(ZipPrefix);
}
