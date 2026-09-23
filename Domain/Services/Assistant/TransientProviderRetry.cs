// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared transient-failure retry around a single provider call, used by the chat pipeline (LLMService)
/// and by the pipeline-free one-shot completion path so both retry the same errors with the same backoff.
/// Retries only failed responses whose error TransientProviderErrorDetector classifies as transient
/// (rate limit, overload, gateway errors), with LLMRetryConstants' linear backoff; non-transient errors
/// and exhausted retries return the failed response as-is.
/// </summary>
/// <param name="provider">The LLM provider to call</param>
/// <param name="request">The provider request to (re-)send</param>
/// <param name="logger">Receives one warning per retried attempt</param>
/// <param name="cancellationToken">Passed to the provider and cancels the backoff delay between attempts</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public static class TransientProviderRetry
{
    public static async Task<LLMProviderResponse> ProcessAsync(
        ILLMProvider provider,
        LLMProviderRequest request,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var response = await provider.ProcessAsync(request, cancellationToken);

        for (var attempt = 1;
             !response.Success
                 && attempt <= LLMRetryConstants.MaxTransientRetries
                 && TransientProviderErrorDetector.IsTransient(response.Error);
             attempt++)
        {
            logger.LogWarning(
                "Transient provider error (attempt {Attempt}/{Max}): {Error} - retrying",
                attempt, LLMRetryConstants.MaxTransientRetries, response.Error);
            await Task.Delay(LLMRetryConstants.GetRetryDelay(attempt), cancellationToken);
            response = await provider.ProcessAsync(request, cancellationToken);
        }

        return response;
    }
}
