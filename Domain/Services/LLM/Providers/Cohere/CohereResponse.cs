namespace Klacks.Api.Domain.Services.LLM.Providers.Cohere;

public class CohereResponse
{
    public string Text { get; set; } = string.Empty;
    public CohereMeta? Meta { get; set; }
}