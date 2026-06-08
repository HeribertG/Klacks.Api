// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Groq Whisper STT provider factory.
/// Creates GroqWhisperSttSession instances that buffer audio and transcribe via REST.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients used in sessions</param>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.Http.Headers;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public class GroqWhisperSttProvider : ISttProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderId => SttProviderConstants.GroqWhisper;

    public GroqWhisperSttProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<ISttSession> CreateSessionAsync(SttConfig config, CancellationToken ct = default)
    {
        ISttSession session = new GroqWhisperSttSession(_httpClientFactory, config);
        return Task.FromResult(session);
    }

    public async Task ValidateAsync(SttConfig config, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        var response = await client.GetAsync(SttProviderConstants.GroqModelsUrl, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Groq STT validation failed ({(int)response.StatusCode}): {body}");
        }
    }
}
