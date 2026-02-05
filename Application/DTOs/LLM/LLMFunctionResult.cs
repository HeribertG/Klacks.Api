namespace Klacks.Api.Application.DTOs.LLM;

public class LLMFunctionResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}
