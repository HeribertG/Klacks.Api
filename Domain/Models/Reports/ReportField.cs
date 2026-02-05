using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Reports;

public class ReportField
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataBinding { get; set; } = string.Empty;
    public ReportFieldType Type { get; set; } = ReportFieldType.Text;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 100;
    public float Height { get; set; } = 20;
    public FieldStyle Style { get; set; } = new();
    public string? Format { get; set; }
    public string? Formula { get; set; }
    public int SortOrder { get; set; }
}
