namespace Klacks.Api.Resources.Filter;

public class ShiftScheduleFilter
{
    public int DayVisibleBeforeMonth { get; set; }

    public int DayVisibleAfterMonth { get; set; }

    public int CurrentMonth { get; set; }

    public int CurrentYear { get; set; }
}
