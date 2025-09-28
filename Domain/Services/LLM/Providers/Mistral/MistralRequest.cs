using Klacks.Api.Domain.Services.LLM.Providers.Shared;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Services.LLM.Providers.Mistral;

public class MistralRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAIMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
}