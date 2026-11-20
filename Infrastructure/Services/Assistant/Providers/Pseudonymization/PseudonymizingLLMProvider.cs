// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decorator that keeps person data off the line to a cloud language model. Before the call it copies the request
/// and replaces known names, phone numbers, e-mail addresses, personnel numbers and
/// birthdates by pseudonyms, adding a short legend to the volatile system prompt; after the call it puts the real
/// values back into the answer text and into every tool argument, so everything behind it sees plain text only. An
/// invented code in a tool argument is marked so the chat loop rejects the call. A call to a model inside the
/// installation (<see cref="LocalModelEndpoint"/>) passes through unchanged. Fails closed: if the dictionary cannot be
/// built, or the request carries an image, nothing is sent. Only the factory creates it, directly around the concrete
/// provider. Every member of ILLMProvider is forwarded explicitly, default interface members included.
/// </summary>
/// <param name="inner">The concrete provider that makes the real call</param>
/// <param name="baseUrl">The BaseUrl that decides whether the call stays local; null (always cloud) for every provider
/// except the generic OpenAI-compatible one</param>
/// <param name="mapSource">Dictionary and pattern numbers of the installation</param>
/// <param name="companyClock">Company today, for the age that replaces a birthdate</param>

using System.Runtime.CompilerServices;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Services.Assistant.Pseudonymization;
using Klacks.Api.Infrastructure.Services.Assistant.Pseudonymization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Pseudonymization;

public sealed class PseudonymizingLLMProvider : ILLMProvider
{
    private readonly ILLMProvider _inner;
    private readonly IPseudonymMapSource _mapSource;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger _logger;
    private readonly string? _baseUrl;

    public PseudonymizingLLMProvider(
        ILLMProvider inner,
        string? baseUrl,
        IPseudonymMapSource mapSource,
        ICompanyClock companyClock,
        ILogger logger)
    {
        _inner = inner;
        _baseUrl = baseUrl;
        _mapSource = mapSource;
        _companyClock = companyClock;
        _logger = logger;
    }

    internal ILLMProvider Inner => _inner;

    public string ProviderId => _inner.ProviderId;

    public string ProviderName => _inner.ProviderName;

    public bool IsEnabled => _inner.IsEnabled;

    public bool SupportsStreaming => _inner.SupportsStreaming;

    public bool SupportsToolChoice => _inner.SupportsToolChoice;

    public ForcedToolChoiceSupport ResolveForcedToolChoiceSupport(LLMProviderRequest request) =>
        _inner.ResolveForcedToolChoiceSupport(request);

    public void Configure(Domain.Models.Assistant.LLMProvider providerConfig) => _inner.Configure(providerConfig);

    public Task<bool> ValidateApiKeyAsync(string apiKey) => _inner.ValidateApiKeyAsync(apiKey);

    public int GetEffectiveInputTokenLimit(Domain.Models.Assistant.LLMModel model) =>
        _inner.GetEffectiveInputTokenLimit(model);

    public Task<List<Domain.Models.Assistant.LLMModelDiscovery>?> GetAvailableModelsAsync() =>
        _inner.GetAvailableModelsAsync();

    public Task<Domain.Models.Assistant.LLMModelTestResult> TestModelAsync(string apiModelId, string? supportedParameters = null) =>
        _inner.TestModelAsync(apiModelId, supportedParameters);

    public async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsActiveFor(request))
        {
            return await _inner.ProcessAsync(request, cancellationToken);
        }

        if (request.ImagePng != null)
        {
            return new LLMProviderResponse { Success = false, Error = PseudonymizationConstants.ImageRefusedError };
        }

        PreparedRequest prepared;
        try
        {
            prepared = await PrepareAsync(request, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Pseudonymisation failed; the request to {ProviderId} was not sent", ProviderId);
            return new LLMProviderResponse { Success = false, Error = PseudonymizationConstants.DictionaryUnavailableError };
        }

        var response = await _inner.ProcessAsync(prepared.Request, cancellationToken);
        var unknownTokens = new List<string>();
        response.Content = PseudonymRestorer.RestoreText(response.Content, prepared.Map, unknownTokens);
        foreach (var call in response.FunctionCalls)
        {
            call.Parameters = PseudonymRestorer.RestoreParameters(call.Parameters, prepared.Map, unknownTokens);
        }

        LogUnknown(unknownTokens);
        return response;
    }

    public async IAsyncEnumerable<string> ProcessStreamAsync(
        LLMProviderRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsActiveFor(request))
        {
            await foreach (var token in _inner.ProcessStreamAsync(request, cancellationToken))
            {
                yield return token;
            }

            yield break;
        }

        if (request.ImagePng != null)
        {
            throw new InvalidOperationException(PseudonymizationConstants.ImageRefusedError);
        }

        PreparedRequest prepared;
        try
        {
            prepared = await PrepareAsync(request, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Pseudonymisation failed; the streaming request to {ProviderId} was not sent", ProviderId);
            throw new InvalidOperationException(PseudonymizationConstants.DictionaryUnavailableError, ex);
        }

        var unknownTokens = new List<string>();
        var restorer = new PseudonymStreamRestorer(prepared.Map, unknownTokens);
        await foreach (var token in _inner.ProcessStreamAsync(prepared.Request, cancellationToken))
        {
            foreach (var output in restorer.Push(token))
            {
                yield return output;
            }
        }

        foreach (var output in restorer.Flush())
        {
            yield return output;
        }

        LogUnknown(unknownTokens);
    }

    internal bool IsActiveFor(LLMProviderRequest request) => !LocalModelEndpoint.IsLocal(_baseUrl, request.ModelId);

    private async Task<PreparedRequest> PrepareAsync(LLMProviderRequest request, CancellationToken cancellationToken)
    {
        var dictionary = await _mapSource.GetDictionaryAsync(cancellationToken);
        var patternValues = Texts(request)
            .SelectMany(text => PseudonymTextPseudonymizer.FindPatternValues(text, dictionary))
            .ToList();
        var map = new PseudonymMap(dictionary, patternValues);
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);

        var usedBaseTokens = new HashSet<string>(StringComparer.Ordinal);
        var copy = PseudonymRequestCopier.Copy(
            request,
            text => PseudonymTextPseudonymizer.Pseudonymize(text, map, usedBaseTokens, today),
            text => PseudonymTextPseudonymizer.Pseudonymize(text, map, usedBaseTokens, today, requireCapitalSingleWord: true));

        var legend = PseudonymLegend.Build(usedBaseTokens, usedBaseTokens.Count > 0 || patternValues.Count > 0, dictionary);
        if (legend != null)
        {
            copy.VolatileSystemPrompt = string.IsNullOrEmpty(copy.VolatileSystemPrompt)
                ? legend
                : $"{copy.VolatileSystemPrompt}\n\n{legend}";
        }

        return new PreparedRequest(copy, map);
    }

    private static IEnumerable<string?> Texts(LLMProviderRequest request)
    {
        yield return request.SystemPrompt;
        foreach (var message in request.ConversationHistory)
        {
            yield return message.Content;
        }

        yield return request.Message;
        yield return request.VolatileSystemPrompt;
    }

    private void LogUnknown(List<string> unknownTokens)
    {
        if (unknownTokens.Count > 0)
        {
            _logger.LogWarning(
                "Model {ProviderId} used {Count} pseudonym code(s) that do not exist; they were not restored",
                ProviderId, unknownTokens.Count);
        }
    }

    private sealed record PreparedRequest(LLMProviderRequest Request, PseudonymMap Map);
}
