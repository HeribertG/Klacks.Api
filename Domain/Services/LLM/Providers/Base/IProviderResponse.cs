namespace Klacks.Api.Domain.Services.LLM.Providers.Base;

public interface IProviderResponse
{
    bool IsValid { get; }
    string GetContent();
    int GetInputTokens();
    int GetOutputTokens();
}