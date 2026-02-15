namespace Klacks.Api.Domain.Models.Reports;

public class FreeTextRow
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Position { get; set; } = "after";
    public FieldStyle Style { get; set; } = new();
}
