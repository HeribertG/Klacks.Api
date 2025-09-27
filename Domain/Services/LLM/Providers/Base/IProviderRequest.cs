namespace Klacks.Api.Domain.Services.LLM.Providers.Base;

public interface IProviderRequest
{
    string Model { get; set; }
    double Temperature { get; set; }
    int MaxTokens { get; set; }
}