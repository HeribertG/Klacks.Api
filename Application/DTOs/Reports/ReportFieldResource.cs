namespace Klacks.Api.Application.DTOs.Reports;

public class ReportFieldResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataBinding { get; set; } = string.Empty;
    public int Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 100;
    public float Height { get; set; } = 20;
    public FieldStyleResource Style { get; set; } = new();
    public string? Format { get; set; }
    public string? Formula { get; set; }
    public int SortOrder { get; set; }
    public string? ImageUrl { get; set; }
}
