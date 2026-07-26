// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;
using Microsoft.Extensions.Configuration;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.OpenAI;

public class OpenAIProvider : BaseOpenAICompatibleProvider
{
    private readonly IConfiguration _configuration;

    public override string ProviderId => _providerConfig!.ProviderId;
    public override string ProviderName => _providerConfig!.ProviderName;

    // Verified against the OpenAI Chat Completions API, which documents stream_options.include_usage.
    protected override bool SupportsStreamUsage => true;

    public OpenAIProvider(HttpClient httpClient, ILogger<OpenAIProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override Task<List<LLMModelDiscovery>?> GetAvailableModelsAsync() => GetModelsFromOpenAIApiAsync();
}