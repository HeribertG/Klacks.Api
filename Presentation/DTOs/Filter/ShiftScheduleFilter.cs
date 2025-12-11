namespace Klacks.Api.Presentation.DTOs.Filter;

public class ShiftScheduleFilter
{
    public int DayVisibleBeforeMonth { get; set; }

    public int DayVisibleAfterMonth { get; set; }

    public int CurrentMonth { get; set; }

    public int CurrentYear { get; set; }

    public List<DateTime>? HolidayDates { get; set; }

    public Guid? SelectedGroup { get; set; }

    public string? SearchString { get; set; }

    public string? OrderBy { get; set; }

    public string? SortOrder { get; set; }

    public int? ShiftType { get; set; }

    public bool? IsSporadic { get; set; }

    public bool? IsTimeRange { get; set; }
}
