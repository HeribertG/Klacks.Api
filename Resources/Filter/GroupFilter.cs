namespace Klacks_api.Resources.Filter
{
  public class GroupFilter : BaseFilter
  {
    public bool ActiveDateRange { get; set; }

    public bool FormerDateRange { get; set; }

    public bool FutureDateRange { get; set; }

    public string SearchString { get; set; } = string.Empty;
  }
}
