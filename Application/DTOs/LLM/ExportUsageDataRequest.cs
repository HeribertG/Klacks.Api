namespace Klacks.Api.Application.DTOs.LLM;

public class ExportUsageDataRequest
{
    public string Format { get; set; } = "csv";
    public int Days { get; set; } = 30;
    public string? UserId { get; set; }
}