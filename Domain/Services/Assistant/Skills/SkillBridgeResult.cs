namespace Klacks.Api.Domain.Services.Assistant.Skills;

public class SkillBridgeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string ResultType { get; set; } = "Data";
}
