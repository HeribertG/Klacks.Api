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

    public bool IsSporadic { get; set; } = true;

    public bool IsTimeRange { get; set; } = true;

    public bool Container { get; set; } = true;

    public bool IsStandartShift { get; set; } = true;

    public bool ShowUngroupedShifts { get; set; } = false;

    public int StartRow { get; set; } = 0;

    public int RowCount { get; set; } = 100;

    public DateTime? SpecificDate { get; set; }

    public Guid? ShiftId { get; set; }
}
