// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Acts on the turn-intent classifier in the one direction that is cheap to get wrong: a confident Ask in a locale
/// the trainer enabled guarantees the best-ranked retrieved Explain skills, so an explanation question keeps its
/// curated explain tool even when keyword and recipe guarantees fill the toolset. A Do prediction never feeds
/// anything here - Do also covers reading and navigating, so it must not be mistaken for a mutation signal. The
/// same turn logs, without acting, whether a confident Ask would have vetoed the detector's forced tool call
/// (tool_choice=required), so that decision can later be made on production numbers. Logs carry the
/// conversation id and the class probabilities, never the message text. A turn that answers a clarification
/// (pinned skills) is not classified: its toolset is already fixed by the pin and the answer is rarely a question.
/// </summary>
/// <param name="classifier">Turn-intent classifier; null predictions leave the turn unchanged</param>
/// <param name="options">Mode: Shadow logs only, Active also returns the guaranteed skill names</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
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

    public async Task<IReadOnlyList<string>> ResolveAsync(
        SkillToolsetGuaranteeRequest request, CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled || request.PinnedSkillNames is { Count: > 0 })
        {
            return [];
        }

        TurnIntentPrediction? prediction;
        try
        {
            prediction = await _classifier.ClassifyAsync(request.UserMessage, request.Language, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Prefix} classification failed; continuing without it", TurnIntentDefaults.LogPrefix);
            return [];
        }

        if (prediction == null)
        {
            return [];
        }

        var explainSkillNames = prediction.MayActOnAsk
            ? TopRetrievedExplainSkillNames(request)
            : [];
        var detectorMutation = MutationIntentDetector.IsMutationIntent(request.UserMessage);

        _logger.LogInformation(
            "{Prefix} mode={Mode} conversation={ConversationId} language={Language} locale={Locale} localeEnabled={LocaleEnabled} " +
            "label={Label} pAsk={AskProbability:F3} pDo={DoProbability:F3} pOther={OtherProbability:F3} confidentAsk={ConfidentAsk} " +
            "detectorMutation={DetectorMutation} detectorQuestion={DetectorQuestion} wouldVetoForcedToolCall={WouldVeto} explainSkills={ExplainSkills}",
            TurnIntentDefaults.LogPrefix, _options.Mode, request.ConversationId, request.Language, prediction.Locale,
            prediction.LocaleEnabled, prediction.Label, prediction.AskProbability, prediction.DoProbability,
            prediction.OtherProbability, prediction.IsConfidentAsk, detectorMutation,
            MutationIntentDetector.IsInformationQuestion(request.UserMessage),
            detectorMutation && prediction.MayActOnAsk, string.Join(",", explainSkillNames));

        return _options.IsActive ? explainSkillNames : [];
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
