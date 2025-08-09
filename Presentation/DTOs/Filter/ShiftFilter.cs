namespace Klacks.Api.Presentation.DTOs.Filter;

public class ShiftFilter : BaseFilter
{
    public bool ActiveDateRange { get; set; }

    public bool FormerDateRange { get; set; }

    public bool FutureDateRange { get; set; }

    public string SearchString { get; set; } = string.Empty;

    public bool IsOriginal { get; set; }

    public bool IncludeClientName { get; set; }
}
