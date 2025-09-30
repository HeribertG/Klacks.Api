namespace Klacks.Api.Domain.Models.Filters;

public class BreakFilter
{
    public string SearchString { get; set; } = string.Empty;

    public int CurrentYear { get; set; }

    public Guid? SelectedGroup { get; set; }

    public List<Guid> AbsenceIds { get; set; } = new List<Guid>();
}