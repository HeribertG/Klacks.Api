// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads one streaming provider call to its end: it separates the three token kinds the providers emit
/// (tool-call deltas, the tool-call end marker, and plain content), accumulates the first two and yields
/// only the content tokens to the caller, which is what reaches the client. Transient failures (rate
/// limit, overload) are retried internally, but ONLY while nothing of this call has reached the caller -
/// once a content token was yielded, a retry would duplicate it on screen, so the failure is reported
/// instead. Extracted from the streaming chat loop, where this was the single largest block.
///
/// Content that is nothing but an echo of the tool-call stand-in text is held back and dropped
/// (PlaceholderEchoFilter); a failure while content is still held back counts as "nothing emitted yet".
///
/// One instance reads exactly one provider call: <see cref="Accumulator"/>, <see cref="HasToolEnd"/> and
/// <see cref="Failed"/> describe the call the enumeration just finished and are only valid after it ends.
/// </summary>
using System.Runtime.CompilerServices;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal sealed class ProviderStreamReader
{
    private const string ToolCallIndexProperty = "index";
    private const string ToolCallNameProperty = "name";
    private const string ToolCallArgumentsProperty = "arguments";

    private readonly ILogger _logger;

    /// <param name="logger">The chat service's logger, so log categories stay unchanged.</param>
    internal ProviderStreamReader(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>Content and tool-call deltas of the finished call.</summary>
    internal StreamAccumulator Accumulator { get; private set; } = new();

    /// <summary>True when the provider signalled that its tool-call deltas are complete.</summary>
    internal bool HasToolEnd { get; private set; }

    /// <summary>True when the call ended on an error that could not (or must not) be retried.</summary>
    internal bool Failed { get; private set; }

    /// <summary>
    /// True when the call was cut short because the caller's token was cancelled - a stop request or a dropped
    /// connection. Neither is an error: nothing is logged, nothing is retried and <see cref="Failed"/> stays
    /// false. The content streamed so far stays in <see cref="Accumulator"/>, unfinished tool-call deltas
    /// included, which the caller must not treat as complete calls.
    /// </summary>
    internal bool Cancelled { get; private set; }

    /// <summary>
    /// Yields the plain content tokens of one provider call, in the order the provider produced them.
    /// </summary>
    /// <param name="provider">The streaming provider; the caller has checked SupportsStreaming.</param>
    /// <param name="request">The prepared request, re-sent unchanged on every transient retry.</param>
    /// <param name="modelId">The provider-side model id, for the error log only.</param>
    /// <param name="cancellationToken">Cancels the provider stream and the backoff delay.</param>
    internal async IAsyncEnumerable<string> ReadAsync(
        ILLMProvider provider,
        LLMProviderRequest request,
        string modelId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var transientAttempt = 0;

        while (true)
        {
            Accumulator = new StreamAccumulator();
            HasToolEnd = false;
            string? streamErrorMessage = null;
            var contentEmitted = false;
            var echoFilter = new PlaceholderEchoFilter();
            var enumerator = provider.ProcessStreamAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);

            try
            {
                while (true)
                {
                    string? token;
                    try
                    {
                        if (!await enumerator.MoveNextAsync()) break;
                        token = enumerator.Current;
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested)
                    {
                        Cancelled = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Streaming provider error for model {ModelId}", modelId);
                        // Kept raw for the transient-error classification and the retry log below;
                        // the client only ever sees the generic text.
                        streamErrorMessage = ex.Message;
                        break;
                    }

                    // A provider that does not observe its token keeps producing; the caller's cancellation
                    // is honoured at the next token regardless.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Cancelled = true;
                        break;
                    }

                    if (token.StartsWith(LLMStreamingTokens.ToolCallPrefix))
                    {
                        AppendToolCallDelta(token);
                    }
                    else if (token == LLMStreamingTokens.ToolCallEnd)
                    {
                        HasToolEnd = true;
                    }
                    else if (echoFilter.Push(token) is { Length: > 0 } visible)
                    {
                        Accumulator.AppendContent(visible);
                        contentEmitted = true;
                        yield return visible;
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            if (Cancelled)
            {
                yield break;
            }

            if (streamErrorMessage == null)
            {
                if (echoFilter.Flush() is { Length: > 0 } remainder)
                {
                    Accumulator.AppendContent(remainder);
                    yield return remainder;
                }

                yield break;
            }

            if (!contentEmitted
                && transientAttempt < LLMRetryConstants.MaxTransientRetries
                && TransientProviderErrorDetector.IsTransient(streamErrorMessage))
            {
                transientAttempt++;
                _logger.LogWarning(
                    "Transient streaming provider error (attempt {Attempt}/{Max}): {Error} - retrying",
                    transientAttempt, LLMRetryConstants.MaxTransientRetries, streamErrorMessage);
                try
                {
                    await Task.Delay(LLMRetryConstants.GetRetryDelay(transientAttempt), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Cancelled = true;
                    yield break;
                }

                continue;
            }

            Failed = true;
            yield break;
        }
    }

    private void AppendToolCallDelta(string token)
    {
        var toolJson = token[LLMStreamingTokens.ToolCallPrefix.Length..];
        try
        {
            var toolData = JsonSerializer.Deserialize<JsonElement>(toolJson);
            var index = toolData.TryGetProperty(ToolCallIndexProperty, out var idx) ? idx.GetInt32() : 0;
            var name = toolData.TryGetProperty(ToolCallNameProperty, out var n) ? n.GetString() : null;
            var args = toolData.TryGetProperty(ToolCallArgumentsProperty, out var a) ? a.GetString() : null;
            Accumulator.AppendToolCallDelta(index, name, args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse tool-call delta from streaming token; token skipped");
        }
    }
}
