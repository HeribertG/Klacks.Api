namespace Klacks.Api.Presentation.DTOs.LLM;

public class LLMFunction
{
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public object Parameters { get; set; } = new();
}