namespace Klacks.Api.Domain.Models.Filters;

public class ShiftFilter
{
    public string SearchString { get; set; } = string.Empty;

    public bool ActiveDateRange { get; set; }

    public bool FormerDateRange { get; set; }

    public bool FutureDateRange { get; set; }

    public bool IsOriginal { get; set; }

    public bool IncludeClientName { get; set; }

    public bool IsSealedOrder { get; set; }
}