// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;
using Microsoft.Extensions.Configuration;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.OpenAI;

public class OpenAIProvider : BaseOpenAICompatibleProvider
{
    private readonly IConfiguration _configuration;

    public override string ProviderId => _providerConfig!.ProviderId;
    public override string ProviderName => _providerConfig!.ProviderName;

    public OpenAIProvider(HttpClient httpClient, ILogger<OpenAIProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }
}