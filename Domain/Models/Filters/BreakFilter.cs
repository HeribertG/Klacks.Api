namespace Klacks.Api.Domain.Models.Filters;

public class BreakFilter
{
    public string SearchString { get; set; } = string.Empty;

    public int CurrentYear { get; set; }

    public string OrderBy { get; set; } = "name";

    public string SortOrder { get; set; } = "asc";

    public Guid? SelectedGroup { get; set; }

    public List<Guid> AbsenceIds { get; set; } = new List<Guid>();

    public int? StartRow { get; set; }

    public int? RowCount { get; set; }
}