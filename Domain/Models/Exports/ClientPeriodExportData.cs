// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Root export data model containing all client groups for a date-range based client period export.
/// @param Clients - List of client groups, each representing an employee or external employee with their work entries
/// @param StartDate - Requested lower bound of the export period
/// @param EndDate - Requested upper bound of the export period
/// @param ExportDate - Timestamp when the export was generated
/// </summary>
namespace Klacks.Api.Domain.Models.Exports;

public class ClientPeriodExportData
{
    public List<ClientPeriodGroup> Clients { get; set; } = [];

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
}
