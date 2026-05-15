// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lightweight projection of a sealed order (Shift with Status=SealedOrder) for the
/// export selection dropdown. Carries customer information and Work-locking statistics
/// so the user can see which orders are ready to be exported.
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class SealedOrderListItem
{
    public Guid Id { get; set; }

    public string Abbreviation { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateOnly FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public Guid? CustomerId { get; set; }

    public int? CustomerNumber { get; set; }

    public string? CustomerName { get; set; }

    public int TotalWorks { get; set; }

    public int ClosedWorks { get; set; }
}
