// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Closing guard of both chat loops: a turn that ran at least one successful tool call must not end with
/// an empty answer or with nothing but echoed tool-call stand-in text (live case: DeepSeek answered its
/// third iteration with exactly "[Executing function calls]", which was stored as the answer). The
/// decision looks at the LAST provider call only, so narration streamed alongside an earlier tool call
/// ("Let me check that...") cannot hide an empty final answer. It makes exactly ONE extra tool-less model
/// call asking for the answer from the tool results already in the conversation, and falls back to a
/// notice localized to the turn's language when that call fails, breaks off mid-stream or is empty again.
/// It never calls the model when no tool ran, when every call failed (the step-failed notice covers that
/// turn), or when the turn deliberately ended on a UiPassthrough batch, which has no prose by design.
/// </summary>
/// <param name="logger">The chat service's logger, so log categories stay unchanged.</param>
/// <param name="provider">The turn's provider.</param>
/// <param name="totalUsage">The turn's usage record; the extra call is added to it.</param>
/// <param name="language">The turn's language, used to localize the fallback notice.</param>
/// <param name="requestFor">Builds the tool-less request from the recovery instruction.</param>
using System.Runtime.CompilerServices;
using System.Text;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal sealed class EmptyAnswerRecovery
{
    private readonly ILogger _logger;
    private readonly ILLMProvider _provider;
    private readonly LLMUsage _totalUsage;
    private readonly string? _language;
    private readonly Func<string, LLMProviderRequest> _requestFor;

    internal EmptyAnswerRecovery(
        ILogger logger, ILLMProvider provider, LLMUsage totalUsage, string? language,
        Func<string, LLMProviderRequest> requestFor)
    {
        _logger = logger;
        _provider = provider;
        _totalUsage = totalUsage;
        _language = language;
        _requestFor = requestFor;
    }

    /// <summary>
    /// Everything <see cref="StreamAsync"/> yielded, i.e. what the turn's answer has to be extended by;
    /// valid once the enumeration ended.
    /// </summary>
    internal string AppendedText { get; private set; } = string.Empty;

    /// <summary>
    /// True when the turn's answer has to be recovered with an extra model call.
    /// </summary>
    /// <param name="lastCallContent">The content of the loop's last provider call, not of the whole turn.</param>
    /// <param name="allFunctionCalls">Every call of the turn.</param>
    /// <param name="endedOnUiPassthrough">Reports whether the loop ended on a UiPassthrough-only batch; read only when needed.</param>
    internal static bool NeedsRecovery(
        string? lastCallContent, IReadOnlyList<LLMFunctionCall> allFunctionCalls, Func<bool> endedOnUiPassthrough) =>
        AnswerPlaceholder.IsBlankOrPlaceholder(lastCallContent)
        && allFunctionCalls.Any(call => call.Success)
        && !endedOnUiPassthrough();

    /// <summary>
    /// Non-streaming path: the visible answer, recovered when necessary.
    /// </summary>
    /// <param name="answer">The content of the loop's last provider call, which is the turn's answer.</param>
    /// <param name="allFunctionCalls">Every call of the turn.</param>
    /// <param name="endedOnUiPassthrough">Reports whether the loop ended on a UiPassthrough-only batch; read only when needed.</param>
    /// <param name="cancellationToken">Cancels the extra call.</param>
    internal async Task<string> ResolveAsync(
        string? answer,
        IReadOnlyList<LLMFunctionCall> allFunctionCalls,
        Func<bool> endedOnUiPassthrough,
        CancellationToken cancellationToken)
    {
        if (!NeedsRecovery(answer, allFunctionCalls, endedOnUiPassthrough))
        {
            return AnswerPlaceholder.Visible(answer);
        }

        LogRecovery(allFunctionCalls.Count);
        return await RequestAnswerAsync(cancellationToken);
    }

    /// <summary>
    /// Streaming path, entered only after the caller decided with <see cref="NeedsRecovery"/>: yields the
    /// text to append below what is already on screen and leaves it in <see cref="AppendedText"/>. Text
    /// already shown is never retracted: a recovery stream that breaks off keeps its streamed part and
    /// gets the fallback notice appended, so the stored answer always equals what the user saw.
    /// </summary>
    /// <param name="functionCallCount">Number of calls of the turn, for the log only.</param>
    /// <param name="continuesEarlierContent">True when earlier content of the turn is on screen, so the recovered text starts a new paragraph.</param>
    /// <param name="cancellationToken">Cancels the extra call.</param>
    internal async IAsyncEnumerable<string> StreamAsync(
        int functionCallCount,
        bool continuesEarlierContent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LogRecovery(functionCallCount);
        var separator = continuesEarlierContent ? EmptyAnswerRecoveryConstants.AppendedAnswerSeparator : string.Empty;

        if (!_provider.SupportsStreaming)
        {
            AppendedText = separator + await RequestAnswerAsync(cancellationToken);
            yield return AppendedText;
            yield break;
        }

        var request = _requestFor(EmptyAnswerRecoveryConstants.RecoveryInstruction);
        request.Stream = true;
        request.OnStreamUsage = usage => LLMUsageAccumulator.Add(_totalUsage, usage);

        var shown = new StringBuilder();
        var reader = new ProviderStreamReader(_logger);
        await foreach (var token in reader.ReadAsync(_provider, request, request.ModelId, cancellationToken))
        {
            var piece = shown.Length == 0 ? separator + token : token;
            shown.Append(piece);
            yield return piece;
        }

        if (reader.Failed || shown.Length == 0)
        {
            if (reader.Failed)
            {
                _logger.LogWarning("Empty-answer recovery stream failed; appending the fallback notice");
            }

            var notice = (shown.Length == 0 ? separator : EmptyAnswerRecoveryConstants.AppendedAnswerSeparator)
                + LocalizedFallbackNotice();
            shown.Append(notice);
            yield return notice;
        }

        AppendedText = shown.ToString();
    }

    /// <summary>
    /// The one tool-less recovery call without streaming: its visible answer, or the fallback notice when
    /// the call fails or answers with nothing but stand-in text.
    /// </summary>
    private async Task<string> RequestAnswerAsync(CancellationToken cancellationToken)
    {
        var recovered = string.Empty;
        try
        {
            var response = await TransientProviderRetry.ProcessAsync(
                _provider, _requestFor(EmptyAnswerRecoveryConstants.RecoveryInstruction), _logger, cancellationToken);
            LLMUsageAccumulator.Add(_totalUsage, response.Usage);
            recovered = response.Success ? AnswerPlaceholder.Visible(response.Content) : string.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Empty-answer recovery call failed; answering with the fallback notice");
        }

        return recovered.Length > 0 ? recovered : LocalizedFallbackNotice();
    }

    /// <summary>
    /// The turn's language resolved through GracefulCorrectionTexts, so an installed language never shows
    /// the English notice; only a language Klacks does not ship at all falls through to the constant.
    /// </summary>
    private string LocalizedFallbackNotice() =>
        GracefulCorrectionTexts.TryGetText(GracefulCorrectionTexts.EmptyAnswerFallbackNotice, _language, out var text)
            ? text
            : EmptyAnswerRecoveryConstants.FallbackNotice;

    private void LogRecovery(int functionCallCount) =>
        _logger.LogWarning(
            "Turn with {Count} function call(s) ended without an answer; making one tool-less recovery call",
            functionCallCount);
}
