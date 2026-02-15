using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Reports;

public class ReportSection
{
    public Guid Id { get; set; }
    public ReportSectionType Type { get; set; }
    public float Height { get; set; }
    public List<ReportField> Fields { get; set; } = [];
    public bool Visible { get; set; } = true;
    public int SortOrder { get; set; }
    public string? BackgroundColor { get; set; }
    public List<ReportField>? TableFooterFields { get; set; }
    public List<FreeTextRow>? FreeTextRows { get; set; }
}
