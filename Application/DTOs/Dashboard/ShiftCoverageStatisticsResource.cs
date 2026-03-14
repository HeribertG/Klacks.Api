// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Statistik-Ressource für Shift-Abdeckung und Versiegelungsstatus pro Gruppe.
/// </summary>
/// <param name="GroupId">ID der Gruppe</param>
/// <param name="GroupName">Name der Gruppe</param>
/// <param name="TotalSlots">Summe der Quantity über alle Shift-Tage</param>
/// <param name="CoveredSlots">Summe der Engaged-Slots</param>
/// <param name="TotalWorkEntries">Anzahl aller Work-Einträge im Zeitraum</param>
/// <param name="SealedWorkEntries">Anzahl der Work-Einträge mit LockLevel >= Confirmed</param>
namespace Klacks.Api.Application.DTOs.Dashboard;

public class ShiftCoverageStatisticsResource
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int TotalSlots { get; set; }
    public int CoveredSlots { get; set; }
    public int TotalWorkEntries { get; set; }
    public int SealedWorkEntries { get; set; }
}
