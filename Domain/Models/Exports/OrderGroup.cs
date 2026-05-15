// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Groups all work entries belonging to a single sealed order.
/// Order metadata (name, abbreviation, period) is always sourced from the
/// SealedOrder, not from any clone, so downstream systems see stable values
/// even after the OriginalShift was renamed or split.
/// @param OrderShiftId - ID of the sealed-order shift (Status = SealedOrder)
/// @param OrderName - Display name from the sealed order
/// @param CustomerId - Identifier of the customer client linked to the sealed order
/// @param WorkEntries - All closed work entries assigned to this order
/// </summary>
namespace Klacks.Api.Domain.Models.Exports;

public class OrderGroup
{
    public Guid OrderShiftId { get; set; }

    public string OrderName { get; set; } = string.Empty;

    public string OrderAbbreviation { get; set; } = string.Empty;

    public DateOnly? OrderFromDate { get; set; }

    public DateOnly? OrderUntilDate { get; set; }

    public TimeOnly? OrderStartShift { get; set; }

    public TimeOnly? OrderEndShift { get; set; }

    public Guid? CustomerId { get; set; }

    public int? CustomerNumber { get; set; }

    public string? CustomerName { get; set; }

    public List<WorkExportEntry> WorkEntries { get; set; } = [];
}
