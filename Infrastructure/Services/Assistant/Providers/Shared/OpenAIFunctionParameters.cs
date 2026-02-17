namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIFunctionParameters
{
    public string Type { get; set; } = "object";
    public Dictionary<string, object> Properties { get; set; } = new();
    public List<string> Required { get; set; } = new();
}