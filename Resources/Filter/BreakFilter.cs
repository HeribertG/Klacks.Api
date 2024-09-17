namespace Klacks_api.Resources.Filter
{
  public class BreakFilter
  {
    public List<AbsenceTokenFilter> Absences { get; set; } = new List<AbsenceTokenFilter>();

    public int CurrentYear { get; set; }

    public string OrderBy { get; set; } = string.Empty;

    public string Search { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;
  }
}
