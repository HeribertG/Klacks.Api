// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Groups all work entries belonging to a single order (shift).
/// @param OrderShiftId - ID of the original order shift
/// @param OrderName - Display name of the order/shift
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

    public List<WorkExportEntry> WorkEntries { get; set; } = [];
}
