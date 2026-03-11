// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filter für die Client-Availability Client-Liste mit Suchfunktion.
/// </summary>
/// <param name="SearchString">Suchbegriff für Client-Name/Vorname/Firma</param>
/// <param name="SelectedGroup">Optionale Gruppen-ID zur Filterung</param>
/// <param name="StartRow">Start-Index für Paging</param>
/// <param name="RowCount">Anzahl der Ergebnisse pro Seite</param>
namespace Klacks.Api.Application.DTOs.Filter;

public class ClientAvailabilityClientFilter
{
    public string SearchString { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? SelectedGroup { get; set; }
    public string OrderBy { get; set; } = "name";
    public string SortOrder { get; set; } = "asc";
    public bool ShowEmployees { get; set; } = true;
    public bool ShowExtern { get; set; } = true;
    public int StartRow { get; set; } = 0;
    public int RowCount { get; set; } = 200;
}
