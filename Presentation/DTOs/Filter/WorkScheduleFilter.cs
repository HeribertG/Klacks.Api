namespace Klacks.Api.Presentation.DTOs.Filter;

public class WorkScheduleFilter
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? SelectedGroup { get; set; }
    public string OrderBy { get; set; } = "name";
    public string SortOrder { get; set; } = "asc";
    public bool ShowEmployees { get; set; } = true;
    public bool ShowExtern { get; set; } = true;
    public string? HoursSortOrder { get; set; }
}
