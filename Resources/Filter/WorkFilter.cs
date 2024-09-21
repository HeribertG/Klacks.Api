using Klacks.Api.Resources.Schedules;

namespace Klacks.Api.Resources.Filter;

public class WorkFilter
{
  public int CurrentMonth { get; set; }

  public int CurrentYear { get; set; }

  public string OrderBy { get; set; } = string.Empty;

  public string Search { get; set; } = string.Empty;

  public string SortOrder { get; set; } = string.Empty;

  public List<WorkResource> Works { get; set; } = new List<WorkResource>();
}
