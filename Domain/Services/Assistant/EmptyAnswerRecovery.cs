// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Closing guard of both chat loops: a turn must not end with an empty answer or with nothing but echoed
/// tool-call stand-in text (live case: DeepSeek answered its third iteration with exactly "[Executing
/// function calls]", which was stored as the answer). It covers a turn that ran at least one successful
/// tool call and a turn that ran no tool at all (a reasoning model that deliberated without writing
/// content left an empty bubble, 2026-09-24). The decision looks at the LAST provider call only, so
/// narration streamed alongside an earlier tool call ("Let me check that...") cannot hide an empty final
/// answer. It makes exactly ONE extra tool-less model call - asking for the answer from the tool results,
/// or for an answer to the user's last message when no tool ran - and falls back to a notice localized to
/// the turn's language when that call fails, breaks off mid-stream or is empty again. That notice says
/// what really happened: after a turn that ran tools that the steps ran, after a turn that ran none that
/// nothing was executed. For the same reason a non-streamed recovery answer of a turn without tool calls
/// that claims a completed action is replaced by the no-action notice - nothing is on screen yet, so the
/// false claim can still be withheld; the streaming path corrects it afterwards through
/// TurnClosingNotices instead. A clarifying question is kept, because it claims nothing. It never calls the
/// model when every call failed (the step-failed notice covers that turn), when the turn deliberately
/// ended on a UiPassthrough batch, which has no prose by design, or when a recipe paused on its
/// confirmation or ask step, whose reply RecipeReplyGuard already made deterministic.
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
    /// True once the turn's answer ended in one of the localized fallback notices instead of a model
    /// answer. The post-turn hooks read it so a canned notice is never learned or remembered as if the
    /// assistant had said it on its own (EmptyAnswerNoticeText documents the persisted side).
    /// </summary>
    internal bool AnsweredWithNotice { get; private set; }

    /// <summary>
    /// True when the turn's answer has to be recovered with an extra model call.
    /// </summary>
    /// <param name="lastCallContent">The content of the loop's last provider call, not of the whole turn.</param>
    /// <param name="allFunctionCalls">Every call of the turn.</param>
    /// <param name="endedOnUiPassthrough">Reports whether the loop ended on a UiPassthrough-only batch; read only when a call ran.</param>
    /// <param name="pausedOnRecipeStep">True when a recipe paused on its confirmation or ask step this turn.</param>
    internal static bool NeedsRecovery(
        string? lastCallContent, IReadOnlyList<LLMFunctionCall> allFunctionCalls, Func<bool> endedOnUiPassthrough,
        bool pausedOnRecipeStep) =>
        AnswerPlaceholder.IsBlankOrPlaceholder(lastCallContent)
        && !pausedOnRecipeStep
        && (allFunctionCalls.Count == 0
            || (allFunctionCalls.Any(call => call.Success) && !endedOnUiPassthrough()));

    /// <summary>
    /// Non-streaming path: the visible answer, recovered when necessary. A recovered answer of a turn
    /// without tool calls that claims a completed action, and is not a question, becomes the no-action notice.
    /// </summary>
    /// <param name="answer">The content of the loop's last provider call, which is the turn's answer.</param>
    /// <param name="allFunctionCalls">Every call of the turn.</param>
    /// <param name="endedOnUiPassthrough">Reports whether the loop ended on a UiPassthrough-only batch; read only when a call ran.</param>
    /// <param name="pausedOnRecipeStep">True when a recipe paused on its confirmation or ask step this turn.</param>
    /// <param name="cancellationToken">Cancels the extra call.</param>
    internal async Task<string> ResolveAsync(
        string? answer,
        IReadOnlyList<LLMFunctionCall> allFunctionCalls,
        Func<bool> endedOnUiPassthrough,
        bool pausedOnRecipeStep,
        CancellationToken cancellationToken)
    {
        if (!NeedsRecovery(answer, allFunctionCalls, endedOnUiPassthrough, pausedOnRecipeStep))
        {
            return AnswerPlaceholder.Visible(answer);
        }

        LogRecovery(allFunctionCalls.Count);
        var recovered = await RequestAnswerAsync(allFunctionCalls.Count, cancellationToken);
        if (allFunctionCalls.Count == 0
            && CompletionClaimDetector.ClaimsCompletion(recovered)
            && !ClarifyingResponse.IsClarifying(recovered))
        {
            _logger.LogWarning("Empty-answer recovery of a turn without tool calls claimed a completed action; " +
                "answering with the no-action notice instead");
            return LocalizedFallbackNotice(allFunctionCalls.Count);
        }

        return recovered;
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
            AppendedText = separator + await RequestAnswerAsync(functionCallCount, cancellationToken);
            yield return AppendedText;
            yield break;
        }

        var request = _requestFor(InstructionFor(functionCallCount));
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
                + LocalizedFallbackNotice(functionCallCount);
            shown.Append(notice);
            yield return notice;
        }

        AppendedText = shown.ToString();
    }

    /// <summary>
    /// The one tool-less recovery call without streaming: its visible answer, or the fallback notice when
    /// the call fails or answers with nothing but stand-in text.
    /// </summary>
    /// <param name="functionCallCount">Number of calls of the turn; selects the recovery instruction.</param>
    /// <param name="cancellationToken">Cancels the extra call.</param>
    private async Task<string> RequestAnswerAsync(int functionCallCount, CancellationToken cancellationToken)
    {
        var recovered = string.Empty;
        try
        {
            var response = await TransientProviderRetry.ProcessAsync(
                _provider, _requestFor(InstructionFor(functionCallCount)), _logger, cancellationToken);
            LLMUsageAccumulator.Add(_totalUsage, response.Usage);
            recovered = response.Success ? AnswerPlaceholder.Visible(response.Content) : string.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Empty-answer recovery call failed; answering with the fallback notice");
        }

        return recovered.Length > 0 ? recovered : LocalizedFallbackNotice(functionCallCount);
    }

    /// <summary>
    /// The fallback notice in the turn's language, resolved through GracefulCorrectionTexts so a language
    /// with a loaded pack shows its own notice; a language the catalogue does not know (no pack loaded, which
    /// also covers a pack directory without assistant-texts.json) falls through to the English constant. A turn without tool calls gets the no-action notice, because "I ran the requested
    /// steps" would be untrue there.
    /// </summary>
    /// <param name="functionCallCount">Number of calls of the turn; selects the notice.</param>
    private string LocalizedFallbackNotice(int functionCallCount)
    {
        AnsweredWithNotice = true;
        var noToolRan = functionCallCount == 0;
        var key = noToolRan
            ? GracefulCorrectionTexts.EmptyAnswerNoActionNotice
            : GracefulCorrectionTexts.EmptyAnswerFallbackNotice;
        if (GracefulCorrectionTexts.TryGetText(key, _language, out var text))
        {
            return text;
        }

        return noToolRan ? EmptyAnswerRecoveryConstants.NoActionNotice : EmptyAnswerRecoveryConstants.FallbackNotice;
    }

    /// <summary>
    /// A turn without tool calls has no tool results to answer from, so its recovery asks for an answer to
    /// the user's last message instead.
    /// </summary>
    private static string InstructionFor(int functionCallCount) =>
        functionCallCount == 0
            ? EmptyAnswerRecoveryConstants.ToolLessRecoveryInstruction
            : EmptyAnswerRecoveryConstants.RecoveryInstruction;

    private void LogRecovery(int functionCallCount) =>
        _logger.LogWarning(
            "Turn with {Count} function call(s) ended without an answer; making one tool-less recovery call",
            functionCallCount);
}
