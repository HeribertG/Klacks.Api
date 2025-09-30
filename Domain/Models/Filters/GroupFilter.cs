namespace Klacks.Api.Domain.Models.Filters;

public class GroupFilter
{
    public string SearchString { get; set; } = string.Empty;

    public bool ActiveDateRange { get; set; }

    public bool FormerDateRange { get; set; }

    public bool FutureDateRange { get; set; }
}