namespace Klacks.Api.Infrastructure.Services.LLM.Providers.Base;

public interface IProviderRequest
{
    string Model { get; set; }
    double Temperature { get; set; }
    int MaxTokens { get; set; }
}