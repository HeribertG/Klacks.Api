using Klacks.Api.Domain.Services.LLM.Providers.Base;
using Microsoft.Extensions.Configuration;

namespace Klacks.Api.Domain.Services.LLM.Providers.Mistral;

public class MistralProvider : BaseOpenAICompatibleProvider
{
    private readonly IConfiguration _configuration;

    public override string ProviderId => "mistral";
    public override string ProviderName => "Mistral AI";

    public MistralProvider(HttpClient httpClient, ILogger<MistralProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }
}