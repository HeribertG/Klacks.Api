namespace Klacks.Api.Domain.Entities.Reports;

public class ReportSection
{
    public Guid Id { get; set; }
    public ReportSectionType Type { get; set; }
    public float Height { get; set; }
    public List<ReportField> Fields { get; set; } = [];
    public bool Visible { get; set; } = true;
    public int SortOrder { get; set; }
    public string? BackgroundColor { get; set; }
}

public enum ReportSectionType
{
    Header = 0,
    PageHeader = 1,
    Detail = 2,
    PageFooter = 3,
    Footer = 4,
    GroupHeader = 5,
    GroupFooter = 6
}
