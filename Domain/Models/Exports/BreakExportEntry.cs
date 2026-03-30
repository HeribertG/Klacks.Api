// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Export entry for a break/absence within the export period.
/// @param AbsenceName - Name of the absence type
/// @param BreakTime - Duration of the break in hours
/// </summary>
namespace Klacks.Api.Domain.Models.Exports;

public class BreakExportEntry
{
    public string AbsenceName { get; set; } = string.Empty;

    public DateOnly BreakDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal BreakTime { get; set; }
}
