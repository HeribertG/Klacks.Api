namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

public interface IProviderRequest
{
    string Model { get; set; }
    double Temperature { get; set; }
    int MaxTokens { get; set; }
}