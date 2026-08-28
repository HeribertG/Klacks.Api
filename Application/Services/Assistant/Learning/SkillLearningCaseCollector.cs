// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns finished chat turns and user corrections - explicit ones and the implicit negation in the
/// following turn - into clustered learning cases. Replaces SkillGapDetector
/// and differs from it in three ways that matter: the cluster key comes from MessageNormalizer, so it is
/// the same key the trajectory capture and the correction endpoint use; the counter lives on the cluster
/// and therefore survives the cluster's own state changes; and the raw message is never stored, only an
/// excerpt of at most 120 characters.
/// Runs fire-and-forget after the turn, so it swallows its own failures - a lost learning case must never
/// surface as a failed chat answer.
/// </summary>
/// <param name="clusterRepository">Cluster store, self-committing</param>
/// <param name="caseRepository">Case store, self-committing</param>
/// <param name="optionsProvider">Settings-backed thresholds</param>
/// <param name="logger">Logger for the swallowed failures</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillLearningCaseCollector : ISkillLearningCaseCollector
{
    private const string UnknownLocale = "??";

    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly ISkillLearningCaseRepository _caseRepository;
    private readonly ISkillLearningOptionsProvider _optionsProvider;
    private readonly ILogger<SkillLearningCaseCollector> _logger;

    public SkillLearningCaseCollector(
        ISkillLearningClusterRepository clusterRepository,
        ISkillLearningCaseRepository caseRepository,
        ISkillLearningOptionsProvider optionsProvider,
        ILogger<SkillLearningCaseCollector> logger)
    {
        _clusterRepository = clusterRepository;
        _caseRepository = caseRepository;
        _optionsProvider = optionsProvider;
        _logger = logger;
    }

    public async Task CollectFromTurnAsync(SkillLearningTurn turn, CancellationToken cancellationToken = default)
    {
        if (turn.HadFunctionCalls
            || AffirmationDetector.IsAffirmation(turn.UserMessage)
            || MessageNormalizer.CountWords(turn.UserMessage) < SkillLearningDefaults.MinTokenCount
            || !RefusalSignalDetector.IsRefusal(turn.AssistantResponse))
        {
            return;
        }

        try
        {
            await RecordAsync(
                turn.AgentId,
                MessageNormalizer.Hash(turn.UserMessage),
                MessageNormalizer.Excerpt(turn.UserMessage, SkillLearningDefaults.ExcerptMaxLength),
                SkillLearningSignals.Refusal,
                turn.UserId,
                turn.ConversationId,
                NormalizeLocale(turn.Language),
                turn.ChosenSkill,
                expectedSkill: null,
                SerializeToolNames(turn.ToolNames),
                trajectoryId: null,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Skill learning case collection failed for agent {AgentId}", turn.AgentId);
        }
    }

    public async Task CollectCorrectionAsync(SkillLearningCorrection correction, CancellationToken cancellationToken = default)
    {
        if (!CorrectionTypeLearningSignals.CorrectionSignals.Contains(correction.Signal, StringComparer.Ordinal))
        {
            return;
        }

        try
        {
            await RecordAsync(
                correction.AgentId,
                MessageNormalizer.Hash(correction.UserMessage),
                MessageNormalizer.Excerpt(correction.UserMessage, SkillLearningDefaults.ExcerptMaxLength),
                correction.Signal,
                correction.UserId,
                conversationId: null,
                NormalizeLocale(correction.Locale),
                correction.ChosenSkill,
                correction.ExpectedSkill,
                toolsetJson: "[]",
                correction.TrajectoryId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception, "Skill learning correction collection failed for agent {AgentId}", correction.AgentId);
        }
    }

    // The excerpt is a prefix of the utterance, so its word count is a lower bound of the real one. The
    // same floor the refusal path uses applies here for a stronger reason: the negation detector matches
    // on single words and would otherwise let "hallo" -> "nein" open a cluster keyed on the greeting.
    public async Task CollectImplicitCorrectionAsync(
        SkillLearningImplicitCorrection correction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correction.ClusterKey)
            || MessageNormalizer.CountWords(correction.IntentExcerpt) < SkillLearningDefaults.MinTokenCount)
        {
            return;
        }

        try
        {
            await RecordAsync(
                correction.AgentId,
                correction.ClusterKey,
                correction.IntentExcerpt,
                SkillLearningSignals.Implicit,
                correction.UserId,
                conversationId: null,
                NormalizeLocale(correction.Locale),
                correction.ChosenSkill,
                expectedSkill: null,
                correction.ToolsetJson,
                correction.TrajectoryId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Skill learning implicit correction collection failed for agent {AgentId}",
                correction.AgentId);
        }
    }

    private async Task RecordAsync(
        Guid agentId,
        string clusterKey,
        string excerpt,
        string signal,
        string? userId,
        string? conversationId,
        string locale,
        string? chosenSkill,
        string? expectedSkill,
        string toolsetJson,
        Guid? trajectoryId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var cluster = await ResolveClusterAsync(agentId, clusterKey, excerpt, locale, now, cancellationToken);
        if (cluster == null || !SkillLearningStateMachine.IsCounting(cluster.Status))
        {
            return;
        }

        // One failed exchange is one case. The refusal path and the implicit-correction path see the same
        // turn from two sides, and an explicit correction lands on top of both, so without this window a
        // single unhappy moment would push a cluster a third of the way to the repetition threshold.
        if (await _caseRepository.HasCaseSinceAsync(
                cluster.Id, userId, now.AddMinutes(-SkillLearningDefaults.DedupWindowMinutes), cancellationToken))
        {
            return;
        }

        await _caseRepository.AddAsync(
            new SkillLearningCase
            {
                Id = Guid.NewGuid(),
                ClusterId = cluster.Id,
                UserId = userId,
                ConversationId = conversationId,
                Locale = locale,
                IntentExcerpt = excerpt,
                Signal = signal,
                ChosenSkill = chosenSkill,
                ExpectedSkill = expectedSkill,
                ToolsetJson = toolsetJson,
                TrajectoryId = trajectoryId,
                IsGolden = cluster.OccurrenceCount == 0,
                OccurredAtUtc = now
            },
            cancellationToken);

        var distinctUsers = await _caseRepository.CountDistinctUsersAsync(cluster.Id, cancellationToken);
        var signalCounts = await _caseRepository.CountBySignalAsync(cluster.Id, cancellationToken);

        await _clusterRepository.RegisterOccurrenceAsync(
            cluster.Id, now, distinctUsers, JsonSerializer.Serialize(signalCounts), cancellationToken);

        await PromoteIfThresholdReachedAsync(cluster, distinctUsers, cancellationToken);
    }

    private async Task<SkillLearningCluster?> ResolveClusterAsync(
        Guid agentId,
        string clusterKey,
        string excerpt,
        string locale,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _clusterRepository.FindByKeyAsync(agentId, clusterKey, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var created = new SkillLearningCluster
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            ClusterKey = clusterKey,
            IntentExcerpt = excerpt,
            Locale = locale,
            OccurrenceCount = 0,
            DistinctUserCount = 0,
            SignalKindsJson = "{}",
            Status = SkillLearningClusterStatuses.Collecting,
            StatusChangedAtUtc = now,
            FirstSeenAtUtc = now,
            LastSeenAtUtc = now
        };

        // A second instance may have inserted the same key between the lookup and the insert. The unique
        // index decides, and the loser simply re-reads the winner's row instead of creating a rival cluster.
        return await _clusterRepository.TryInsertAsync(created, cancellationToken)
            ? created
            : await _clusterRepository.FindByKeyAsync(agentId, clusterKey, cancellationToken);
    }

    // The counters are evaluated from the values this call produced rather than re-read, which keeps the
    // hot path at one round trip. Under concurrency a promotion can therefore be missed here; the daily
    // sweep in SkillLearningBackgroundService promotes whatever this shortcut left behind.
    private async Task PromoteIfThresholdReachedAsync(
        SkillLearningCluster cluster, int distinctUsers, CancellationToken cancellationToken)
    {
        if (cluster.Status != SkillLearningClusterStatuses.Collecting)
        {
            return;
        }

        var options = await _optionsProvider.GetAsync(cancellationToken);
        var occurrences = cluster.OccurrenceCount + 1;

        if (occurrences < options.MinOccurrences && distinctUsers < options.MinDistinctUsers)
        {
            return;
        }

        await _clusterRepository.TryTransitionAsync(
            cluster.Id,
            SkillLearningClusterStatuses.Collecting,
            SkillLearningClusterStatuses.Ready,
            cancellationToken);
    }

    private static string NormalizeLocale(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return UnknownLocale;
        }

        var trimmed = language.Trim();
        return trimmed.Length <= 8 ? trimmed : trimmed[..8];
    }

    private static string SerializeToolNames(IReadOnlyList<string>? toolNames)
    {
        if (toolNames == null || toolNames.Count == 0)
        {
            return "[]";
        }

        var capped = toolNames.Count > SkillLearningDefaults.ToolsetCandidatesMax
            ? toolNames.Take(SkillLearningDefaults.ToolsetCandidatesMax)
            : toolNames;

        return JsonSerializer.Serialize(capped.Select(name => new { name }));
    }
}
