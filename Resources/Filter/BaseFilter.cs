namespace Klacks.Api.Resources.Filter
{
  public class BaseFilter
  {
    public int? FirstItemOnLastPage { get; set; }

    public bool? IsNextPage { get; set; }

    public bool? IsPreviousPage { get; set; }

    public int? NumberOfItemOnPreviousPage { get; set; }

    public int NumberOfItemsPerPage { get; set; }

    public string OrderBy { get; set; } = string.Empty;

    public int RequiredPage { get; set; } = 0;

    public string SortOrder { get; set; } = string.Empty;
  }
}
