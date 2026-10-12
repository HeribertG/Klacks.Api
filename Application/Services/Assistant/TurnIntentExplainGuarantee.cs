// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Acts on the turn-intent classifier in the direction that is cheap to get wrong. A confident Ask in a locale the
/// trainer enabled guarantees the best-ranked retrieved Explain skills, so an explanation question keeps its curated
/// explain tool even when keyword and recipe guarantees fill the toolset. When the veto is configured, the same
/// confident Ask also lifts the tool call MutationIntentDetector would force because the message contains a
/// state-changing word without a recognised question lead ("Erkläre mir, wie ich einen Vertrag anlege"; a leading
/// question word already keeps the detector quiet): answering a question wrongly costs less than acting wrongly,
/// and the turn keeps every tool, so the model can still act. A Do prediction never feeds anything here - Do also
/// covers reading and navigating, so it must not be mistaken for a mutation signal. The classification can be started
/// before retrieval (Start) so its embedding overlaps it; the timeout then bounds only the wait here. Logs carry the
/// conversation id and the class probabilities, never the message text. A turn that answers a clarification (pinned
/// skills) is not classified: its toolset is already fixed by the pin and the answer is rarely a question.
/// </summary>
/// <param name="classifier">Turn-intent classifier; null predictions leave the turn unchanged</param>
/// <param name="options">Mode (Shadow logs only, Active also acts), timeout and whether the veto is configured</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.TurnIntent;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public sealed class TurnIntentExplainGuarantee : ITurnIntentExplainGuarantee
{
    private readonly ITurnIntentClassifier _classifier;
    private readonly TurnIntentClassifierOptions _options;
    private readonly ILogger<TurnIntentExplainGuarantee> _logger;

    public TurnIntentExplainGuarantee(
        ITurnIntentClassifier classifier,
        TurnIntentClassifierOptions options,
        ILogger<TurnIntentExplainGuarantee> logger)
    {
        _classifier = classifier;
        _options = options;
        _logger = logger;
    }

    public Task<TurnIntentPrediction?>? Start(
        string userMessage, string? language, IReadOnlyCollection<string>? pinnedSkillNames, CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled || pinnedSkillNames is { Count: > 0 } || string.IsNullOrWhiteSpace(userMessage))
        {
            return null;
        }

        // Task.Run: a warm embedding call runs its ONNX inference synchronously on the calling thread, so a
        // direct call would finish the classification before retrieval even starts. Not cancelled on timeout:
        // the inference is not preemptible, and ClassifyUnboundedAsync turns every failure into null.
        return Task.Run(() => _classifier.ClassifyUnboundedAsync(userMessage, language, cancellationToken), cancellationToken);
    }

    public async Task<TurnIntentGuaranteeOutcome> ResolveAsync(
        SkillToolsetGuaranteeRequest request, CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled || request.PinnedSkillNames is { Count: > 0 })
        {
            return TurnIntentGuaranteeOutcome.None;
        }

        TurnIntentPrediction? prediction;
        try
        {
            prediction = request.PendingTurnIntent != null
                ? await AwaitStartedAsync(request.PendingTurnIntent, cancellationToken)
                : await _classifier.ClassifyAsync(request.UserMessage, request.Language, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Prefix} classification failed; continuing without it", TurnIntentDefaults.LogPrefix);
            return TurnIntentGuaranteeOutcome.None;
        }

        if (prediction == null)
        {
            return TurnIntentGuaranteeOutcome.None;
        }

        var explainSkillNames = prediction.MayActOnAsk
            ? TopRetrievedExplainSkillNames(request)
            : [];
        var detectorMutation = MutationIntentDetector.IsMutationIntent(request.UserMessage);
        var wouldVeto = detectorMutation && prediction.MayActOnAsk;
        var vetoApplied = wouldVeto && _options.MayVetoForcedToolCall;

        _logger.LogInformation(
            "{Prefix} mode={Mode} conversation={ConversationId} language={Language} locale={Locale} localeEnabled={LocaleEnabled} " +
            "label={Label} pAsk={AskProbability:F3} pDo={DoProbability:F3} pOther={OtherProbability:F3} confidentAsk={ConfidentAsk} " +
            "detectorMutation={DetectorMutation} detectorQuestion={DetectorQuestion} wouldVetoForcedToolCall={WouldVeto} " +
            "vetoApplied={VetoApplied} explainSkills={ExplainSkills}",
            TurnIntentDefaults.LogPrefix, _options.Mode, request.ConversationId, request.Language, prediction.Locale,
            prediction.LocaleEnabled, prediction.Label, prediction.AskProbability, prediction.DoProbability,
            prediction.OtherProbability, prediction.IsConfidentAsk, detectorMutation,
            MutationIntentDetector.IsInformationQuestion(request.UserMessage),
            wouldVeto, vetoApplied, string.Join(",", explainSkillNames));

        return _options.IsActive
            ? new TurnIntentGuaranteeOutcome(explainSkillNames, vetoApplied)
            : TurnIntentGuaranteeOutcome.None;
    }

    private async Task<TurnIntentPrediction?> AwaitStartedAsync(
        Task<TurnIntentPrediction?> started, CancellationToken cancellationToken)
    {
        try
        {
            return await started.WaitAsync(TimeSpan.FromMilliseconds(_options.TimeoutMilliseconds), cancellationToken);
        }
        catch (TimeoutException)
        {
            _logger.LogInformation(
                "{Prefix} skipped: early classification not ready {TimeoutMs} ms after retrieval",
                TurnIntentDefaults.LogPrefix, _options.TimeoutMilliseconds);
            return null;
        }
    }

    private static List<string> TopRetrievedExplainSkillNames(SkillToolsetGuaranteeRequest request)
    {
        var scores = request.RetrievalScores;
        return request.RetrievedSkills
            .Where(skill => skill.Effect == SkillEffect.Explain)
            .OrderByDescending(skill => scores != null && scores.TryGetValue(skill.Name, out var score) ? score : double.MinValue)
            .ThenBy(skill => skill.Name, StringComparer.Ordinal)
            .Take(TurnIntentDefaults.MaxGuaranteedExplainSkills)
            .Select(skill => skill.Name)
            .ToList();
    }
}
