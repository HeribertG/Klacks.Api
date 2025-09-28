using Klacks.Api.Domain.Services.LLM.Providers.Base;
using Microsoft.Extensions.Configuration;

namespace Klacks.Api.Domain.Services.LLM.Providers.DeepSeek;

public class DeepSeekProvider : BaseOpenAICompatibleProvider
{
    private readonly IConfiguration _configuration;

    public override string ProviderId => _providerConfig?.ProviderId ?? "deepseek";

    public override string ProviderName => _providerConfig?.ProviderName ?? "DeepSeek";

    public DeepSeekProvider(HttpClient httpClient, ILogger<DeepSeekProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }
}