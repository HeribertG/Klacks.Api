// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decorator that charges every chat and streaming call of the wrapped provider against the Playground
/// token budget. Before the call it books an estimate (prompt size plus the requested maximum output) into
/// the global, IP and device buckets and refuses the call when one of them is used up; after the call it
/// replaces the estimate by the usage the provider reported. A call that aborts or reports no usage keeps its
/// estimate, so stopping a turn does not hand the tokens back; only a call the provider refused before any output
/// and without usage (an HTTP or provider error such as insufficient balance) gets its estimate released. A refusal marks the ambient
/// visitor so the controller can answer with PLAYGROUND_TOKEN_LIMIT. Only the factory creates it, and only
/// when the instance is the Playground. Every member of ILLMProvider is forwarded explicitly, default
/// interface members included, because a decorator that omits one silently falls back to the interface default.
/// </summary>
/// <param name="inner">The provider that makes the real call</param>
/// <param name="budget">The Playground token budget the call is charged to</param>

using System.Runtime.CompilerServices;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Playground;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Services.Playground;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Playground;

public sealed class PlaygroundBudgetedLLMProvider : ILLMProvider
{
    private readonly ILLMProvider _inner;
    private readonly IPlaygroundTokenBudget _budget;

    public PlaygroundBudgetedLLMProvider(ILLMProvider inner, IPlaygroundTokenBudget budget)
    {
        _inner = inner;
        _budget = budget;
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
        var visitor = PlaygroundVisitorContext.Current;
        var reservation = _budget.TryReserve(visitor, PlaygroundTokenEstimator.EstimateReservation(request));
        if (reservation is null)
        {
            visitor?.MarkTokenLimitHit();
            return new LLMProviderResponse { Success = false, Error = PlaygroundTokenLimitConstants.LimitReachedMessage };
        }

        var response = await _inner.ProcessAsync(request, cancellationToken);
        var usageReported = Settle(reservation, response.Usage);
        if (!response.Success && !usageReported && !cancellationToken.IsCancellationRequested)
        {
            _budget.Release(reservation);
        }

        return response;
    }

    public async IAsyncEnumerable<string> ProcessStreamAsync(
        LLMProviderRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var visitor = PlaygroundVisitorContext.Current;
        var reservation = _budget.TryReserve(visitor, PlaygroundTokenEstimator.EstimateReservation(request));
        if (reservation is null)
        {
            visitor?.MarkTokenLimitHit();
            throw new PlaygroundTokenLimitExceededException();
        }

        var usageReported = false;
        var originalCallback = request.OnStreamUsage;
        request.OnStreamUsage = usage =>
        {
            usageReported |= Settle(reservation, usage);
            originalCallback?.Invoke(usage);
        };

        try
        {
            var produced = false;
            await using var enumerator = _inner.ProcessStreamAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (Exception ex) when (!produced && !usageReported && !cancellationToken.IsCancellationRequested && IsProviderRefusal(ex))
                {
                    _budget.Release(reservation);
                    throw;
                }

                if (!hasNext)
                {
                    yield break;
                }

                produced = true;
                yield return enumerator.Current;
            }
        }
        finally
        {
            request.OnStreamUsage = originalCallback;
        }
    }

    private static bool IsProviderRefusal(Exception exception) =>
        exception is InvalidOperationException or HttpRequestException;

    private bool Settle(PlaygroundBudgetReservation reservation, LLMUsage? usage)
    {
        var actual = usage is null ? 0 : PlaygroundTokenEstimator.Weigh(usage);
        if (actual <= 0)
        {
            return false;
        }

        _budget.Settle(reservation, actual);
        return true;
    }
}
