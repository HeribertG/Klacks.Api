// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.DTOs.Filter;

public class ShiftFilter : BaseFilter
{
    public bool ActiveDateRange { get; set; }

    public bool FormerDateRange { get; set; }

    public bool FutureDateRange { get; set; }

    /// <summary>
    /// Optional validity period the shift list is narrowed to: shifts whose FromDate..UntilDate (UntilDate empty =
    /// open) overlap it. ValidFrom empty = no lower bound, ValidUntil empty = no upper bound, both empty = no period
    /// filter. Combined with the active/former/future flags; the unsealed-draft view ignores it like those flags.
    /// </summary>
    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidUntil { get; set; }

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
    /// These text fields are what the search_shifts skill sets.
    /// </summary>
    public string? Country { get; set; }

    public string? State { get; set; }

    public string? City { get; set; }

    public string? ZipPrefix { get; set; }

    /// <summary>
    /// Multi-select location filters of the shift list (the codes picked in the country and canton dropdowns): the
    /// address country must be one of Countries and its state one of States; an empty list = no restriction. They are
    /// combined with the text fields above (every set part must hold).
    /// </summary>
    public List<string>? Countries { get; set; }

    public List<ShiftFilterState>? States { get; set; }

    public bool HasLocationFilter =>
        !string.IsNullOrWhiteSpace(Country)
        || !string.IsNullOrWhiteSpace(State)
        || !string.IsNullOrWhiteSpace(City)
        || !string.IsNullOrWhiteSpace(ZipPrefix)
        || Countries is { Count: > 0 }
        || States is { Count: > 0 };
}
