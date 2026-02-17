namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Cohere;

public class CohereResponse
{
    public string Text { get; set; } = string.Empty;
    public CohereMeta? Meta { get; set; }
}