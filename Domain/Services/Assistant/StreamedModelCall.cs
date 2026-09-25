// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One model call of a streamed turn: streams a streaming provider token by token, or makes one
/// non-streaming call for a provider that cannot stream, and hands back what the call produced. Content
/// reaches the client as it arrives; tool-call deltas are collected and completed. The first content
/// token stamps the turn's time to first token, and a failed call is reported through Error instead of
/// an event so the turn decides how it ends. The call runs on a token linked to the request token and the
/// turn's stop token; a call that either token cut short is reported through Cancelled, and tool calls
/// that were still being received are then not completed. One instance covers exactly one call.
/// </summary>
/// <param name="logger">The chat service's logger, so log categories stay unchanged</param>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal sealed class StreamedModelCall
{
    private readonly ILogger _logger;

    internal StreamedModelCall(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>Content and complete tool calls of the finished call.</summary>
    internal StreamAccumulator Accumulator { get; private set; } = new();

    /// <summary>Client-facing text of the failure the call ended on, null when it succeeded.</summary>
    internal string? Error { get; private set; }

    /// <summary>True when the request or the stop token cut the call short; not an error.</summary>
    internal bool Cancelled { get; private set; }

    /// <param name="provider">The provider to call</param>
    /// <param name="request">The prepared request</param>
    /// <param name="model">The model the call runs on; its API id is used for the error log</param>
    /// <param name="turn">The turn the call belongs to; receives usage and the time to first token</param>
    /// <param name="stopwatch">The turn's clock, the time to first token is read from it</param>
    /// <param name="cancellationToken">The request token; the turn's stop token is linked to it for the call</param>
    internal async IAsyncEnumerable<SseChunk> RunAsync(
        ILLMProvider provider,
        LLMProviderRequest request,
        LLMModel model,
        TurnRunState turn,
        Stopwatch stopwatch,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var hasToolEnd = false;

        if (provider.SupportsStreaming)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, turn.StopToken);
            var reader = new ProviderStreamReader(_logger);
            await foreach (var token in reader.ReadAsync(provider, request, model.ApiModelId, linked.Token))
            {
                if (turn.TtftMs == null)
                {
                    turn.TtftMs = stopwatch.ElapsedMilliseconds;
                    _logger.LogInformation(
                        "LLM TTFT: {Ms}ms turn={Turn}", turn.TtftMs, LLMService.TurnCorrelationFor(turn.Context!));
                }

                yield return SseChunk.Content(token);
            }

            Accumulator = reader.Accumulator;
            if (reader.Cancelled)
            {
                Cancelled = true;
                yield break;
            }

            if (reader.Failed)
            {
                Error = AssistantStreamErrorMessages.ProviderFailure;
                yield break;
            }

            hasToolEnd = reader.HasToolEnd;
        }
        else
        {
            var response = await StopAwareCall.RunAsync(
                turn, cancellationToken, token => TransientProviderRetry.ProcessAsync(provider, request, _logger, token));
            if (response == null)
            {
                Cancelled = true;
                yield break;
            }

            LLMUsageAccumulator.Add(turn.Usage, response.Usage);

            if (!response.Success)
            {
                Error = response.Error ?? "Provider error";
                yield break;
            }

            var visibleContent = AnswerPlaceholder.Visible(response.Content);
            Accumulator.AppendContent(visibleContent);
            yield return SseChunk.Content(visibleContent);
            hasToolEnd = Accumulator.AppendCompleteFunctionCalls(response.FunctionCalls);
        }

        if (hasToolEnd)
        {
            Accumulator.FinalizeFunctionCalls();
        }
    }
}
