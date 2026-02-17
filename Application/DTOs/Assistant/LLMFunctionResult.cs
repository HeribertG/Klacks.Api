namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMFunctionResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}
