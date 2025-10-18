namespace Klacks.Api.Presentation.DTOs.Filter;

public class BreakFilter
{
    public List<AbsenceTokenFilter> Absences { get; set; } = new List<AbsenceTokenFilter>();

    public int CurrentYear { get; set; }

    public string OrderBy { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;

    public Guid? SelectedGroup { get; set; }

    public int? StartRow { get; set; }

    public int? RowCount { get; set; }
}
