namespace Klacks.Api.Presentation.DTOs.Filter;

public class WorkScheduleFilter
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? SelectedGroup { get; set; }
}
