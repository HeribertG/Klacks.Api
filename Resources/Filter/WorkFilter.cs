using Klacks.Api.Resources.Schedules;

namespace Klacks.Api.Resources.Filter;

public class WorkFilter
{
    public int DayVisibleBeforeMonth { get; set; }

    public int DayVisibleAfterMonth { get; set; }

    public int CurrentMonth { get; set; }

    public int CurrentYear { get; set; }

    public string OrderBy { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;

    public List<WorkResource> Works { get; set; } = new List<WorkResource>();

    public Guid? SelectedGroup { get; set; }
}
