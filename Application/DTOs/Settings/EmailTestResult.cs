namespace Klacks.Api.Presentation.DTOs.Settings;

public class EmailTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
}