// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Factory creating CustomRestSttSession instances for user-configured OpenAI-compatible
/// STT endpoints and validating them via the models listing endpoint.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients used in sessions</param>
/// <param name="dictionaryService">Supplies transcription dictionary terms for the Whisper bias prompt</param>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.Http.Headers;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public class CustomRestSttSessionFactory : ICustomSttSessionFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDictionaryService _dictionaryService;

    public CustomRestSttSessionFactory(IHttpClientFactory httpClientFactory, IDictionaryService dictionaryService)
    {
        _httpClientFactory = httpClientFactory;
        _dictionaryService = dictionaryService;
    }

    public ISttSession CreateSession(CustomSttProvider provider, string locale)
    {
        EnsureRestConnectionType(provider);
        return new CustomRestSttSession(_httpClientFactory, provider, _dictionaryService, locale);
    }

    public async Task ValidateAsync(CustomSttProvider provider, CancellationToken ct = default)
    {
        EnsureRestConnectionType(provider);

        var client = _httpClientFactory.CreateClient();
        if (!string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        }

        var url = provider.ApiUrl.TrimEnd('/') + SttProviderConstants.OpenAiModelsPath;
        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Custom STT validation failed ({(int)response.StatusCode}): {body}");
        }
    }

    private static void EnsureRestConnectionType(CustomSttProvider provider)
    {
        if (!string.Equals(provider.ConnectionType, SttProviderConstants.ConnectionTypeRest, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Custom STT connection type '{provider.ConnectionType}' is not supported; only '{SttProviderConstants.ConnectionTypeRest}' is available.");
        }
    }
}
