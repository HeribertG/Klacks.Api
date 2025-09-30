namespace Klacks.Api.Domain.Models.Filters;

public class WorkFilter
{
    public string SearchString { get; set; } = string.Empty;

    public int CurrentMonth { get; set; }

    public int CurrentYear { get; set; }

    public int DayVisibleBeforeMonth { get; set; }

    public int DayVisibleAfterMonth { get; set; }

    public Guid? SelectedGroup { get; set; }
}