using Klacks.Api.Resources.Schedules;

namespace Klacks.Api.Resources.Filter;

public class ShiftFilter : BaseFilter
{
    public bool ActiveDateRange { get; set; }

    public bool FormerDateRange { get; set; }

    public bool FutureDateRange { get; set; }

    public string SearchString { get; set; } = string.Empty;
}
