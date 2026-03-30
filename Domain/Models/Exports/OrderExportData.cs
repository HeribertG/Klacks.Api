// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Root export data model containing all order groups for export.
/// @param Orders - List of order groups, each representing a shift/order with its work entries
/// @param ExportDate - Timestamp when the export was generated
/// </summary>
namespace Klacks.Api.Domain.Models.Exports;

public class OrderExportData
{
    public List<OrderGroup> Orders { get; set; } = [];

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
}
