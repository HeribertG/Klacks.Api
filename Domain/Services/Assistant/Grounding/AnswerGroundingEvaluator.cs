// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Post-turn shadow evaluator against silent hallucination: sanitizes the answer, extracts hard
/// claims, builds the grounding pool from structured tool data plus the legitimate context
/// sources (user message, entity grounding block, injected non-self memories, page context) and
/// persists tiered findings and daily counters. Name candidates that no source covers become
/// tier-2 claims only when the resolver confirms them as real clients; their values are never
/// serialized into the uncovered-claims snapshot. Writes telemetry only — it never triggers a
/// reflection lesson; uncertainty always resolves to silence.
/// </summary>
/// <param name="options">Bound Assistant:AnswerGroundingMode configuration (Off disables everything).</param>
/// <param name="repository">Persistence for findings and daily counters.</param>
/// <param name="memoryRepository">Loads injected memories so their values count as grounded (except agent_self).</param>
/// <param name="nameResolver">Confirms uncovered name candidates against the client database.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Grounding;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Grounding;

public class AnswerGroundingEvaluator : IAnswerGroundingEvaluator
{
    public const int EvaluatorVersion = 1;
    public const int UuidTier = 1;
    public const int NameTier = 2;
    public const string NoToolScopeKey = "answer-grounding";

    private const int LessonRepeatThreshold = 2;
    private const int NumberDateTier = 3;
    private const int MinUncoveredNumbersForFinding = 2;
    private const int MinSignificantDigits = 3;
    private const int ResponseExcerptMaxChars = 1000;
    private const int EvidenceExcerptMaxChars = 4000;

    private readonly AnswerGroundingOptions _options;
    private readonly IAnswerGroundingRepository _repository;
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IAnswerGroundingNameResolver _nameResolver;
    private readonly ILLMBackgroundTaskService _backgroundTasks;
    private readonly ILogger<AnswerGroundingEvaluator> _logger;

    public AnswerGroundingEvaluator(
        AnswerGroundingOptions options,
        IAnswerGroundingRepository repository,
        IAgentMemoryRepository memoryRepository,
        IAnswerGroundingNameResolver nameResolver,
        ILLMBackgroundTaskService backgroundTasks,
        ILogger<AnswerGroundingEvaluator> logger)
    {
        _options = options;
        _repository = repository;
        _memoryRepository = memoryRepository;
        _nameResolver = nameResolver;
        _backgroundTasks = backgroundTasks;
        _logger = logger;
    }

    public async Task EvaluateAsync(
        Guid agentId,
        LLMContext context,
        string responseContent,
        IReadOnlyList<LLMFunctionCall> allFunctionCalls,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            return;
        }

        var delta = new AnswerGroundingDailyCounter
        {
            Day = DateOnly.FromDateTime(DateTime.UtcNow),
            AgentId = agentId,
            EvaluatorVersion = EvaluatorVersion
        };

        try
        {
            if (allFunctionCalls.Any(c => !c.Success && !c.RequiresConfirmation))
            {
                delta.TurnsSkipped = 1;
                return;
            }

            delta.TurnsEvaluated = 1;

            var sanitized = AnswerGroundingResponseSanitizer.Sanitize(responseContent);
            var claims = AnswerClaimExtractor.Extract(sanitized, context.Language);
            var nameCandidates = AnswerNameCandidateExtractor.Extract(sanitized);
            delta.ClaimsExtracted = claims.Count;

            if (claims.Count == 0 && nameCandidates.Count == 0)
            {
                delta.TurnsClean = 1;
                return;
            }

            var pool = ToolResultGroundingPoolBuilder.Build(
                allFunctionCalls,
                await CollectContextTextsAsync(context, cancellationToken),
                context.Language);

            var uncovered = claims.Where(c => !pool.Covers(c)).ToList();
            var uncoveredNames = await ResolveUncoveredNameClaimsAsync(nameCandidates, pool, cancellationToken);
            uncovered.AddRange(uncoveredNames);
            delta.ClaimsExtracted = claims.Count + uncoveredNames.Count;
            delta.ClaimsUncovered = uncovered.Count;

            var tier = DecideTier(uncovered);
            if (tier == null)
            {
                delta.TurnsClean = 1;
                return;
            }

            delta.TurnsWithFindings = 1;
            var finding = BuildFinding(agentId, context, sanitized, allFunctionCalls, claims.Count, uncovered, tier.Value, pool.EmptyDataDespiteSuccess);
            await _repository.AddFindingAsync(finding, cancellationToken);
            await TriggerLessonIfRepeatedAsync(agentId, context, finding, uncovered, cancellationToken);
        }
        catch (Exception ex)
        {
            delta.TurnsEvaluated = 0;
            delta.TurnsClean = 0;
            delta.TurnsWithFindings = 0;
            delta.TurnsSkipped = 0;
            delta.TurnsNoVerdict = 1;
            _logger.LogWarning(ex, "Answer grounding evaluation failed for agent {AgentId}", agentId);
        }
        finally
        {
            await PersistCountersAsync(delta, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<string?>> CollectContextTextsAsync(LLMContext context, CancellationToken cancellationToken)
    {
        var texts = new List<string?> { context.Message, context.EntityGroundingBlock };

        if (context.PageContext != null)
        {
            texts.Add(JsonSerializer.Serialize(context.PageContext));
        }

        if (context.InjectedMemoryIds is { Count: > 0 })
        {
            var memories = await _memoryRepository.GetByIdsAsync(context.InjectedMemoryIds, cancellationToken);
            texts.AddRange(memories
                .Where(m => !string.Equals(m.Source, MemorySources.AgentSelf, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Content));
        }

        return texts;
    }

    private async Task<List<AnswerClaim>> ResolveUncoveredNameClaimsAsync(
        IReadOnlyList<string> nameCandidates,
        GroundingPool pool,
        CancellationToken cancellationToken)
    {
        var uncoveredCandidates = nameCandidates.Where(c => !pool.CoversName(c)).ToList();
        if (uncoveredCandidates.Count == 0)
        {
            return [];
        }

        var resolved = await _nameResolver.ResolveClientNamesAsync(uncoveredCandidates, cancellationToken);
        return resolved
            .Select(name => new AnswerClaim(AnswerClaimKind.Name, name, []))
            .ToList();
    }

    internal static int? DecideTier(IReadOnlyList<AnswerClaim> uncovered)
    {
        if (uncovered.Any(c => c.Kind == AnswerClaimKind.Uuid))
        {
            return UuidTier;
        }

        if (uncovered.Any(c => c.Kind == AnswerClaimKind.Name))
        {
            return NameTier;
        }

        var significant = uncovered.Count(c =>
            (c.Kind == AnswerClaimKind.Number || c.Kind == AnswerClaimKind.Date)
            && c.Readings.Any(r => r.Count(char.IsDigit) >= MinSignificantDigits));

        return significant >= MinUncoveredNumbersForFinding ? NumberDateTier : null;
    }

    private AnswerGroundingFinding BuildFinding(
        Guid agentId,
        LLMContext context,
        string sanitizedResponse,
        IReadOnlyList<LLMFunctionCall> calls,
        int claimsExtracted,
        IReadOnlyList<AnswerClaim> uncovered,
        int tier,
        bool emptyDataDespiteSuccess)
    {
        var evidence = string.Join("\n---\n", calls
            .Where(c => c.Success && c.DataJson.Count > 0)
            .SelectMany(c => c.DataJson.Select(d => $"{c.FunctionName}: {d}")));

        return new AnswerGroundingFinding
        {
            AgentId = agentId,
            ConversationId = context.ConversationId,
            Tier = tier,
            ScopeKey = calls.FirstOrDefault(c => c.Success && c.DataJson.Count > 0)?.FunctionName ?? NoToolScopeKey,
            PrimaryClaimKind = uncovered.Any(c => c.Kind == AnswerClaimKind.Uuid)
                ? nameof(AnswerClaimKind.Uuid)
                : uncovered.Any(c => c.Kind == AnswerClaimKind.Name)
                    ? nameof(AnswerClaimKind.Name)
                    : nameof(AnswerClaimKind.Number),
            EvaluatorVersion = EvaluatorVersion,
            Mode = _options.Mode,
            ClaimsExtracted = claimsExtracted,
            ClaimsUncovered = uncovered.Count,
            UncoveredClaimsJson = JsonSerializer.Serialize(uncovered.Select(c => new
            {
                Kind = c.Kind.ToString(),
                RawText = c.Kind == AnswerClaimKind.Name ? null : c.RawText,
                Readings = c.Kind == AnswerClaimKind.Name ? (IReadOnlyList<string>)Array.Empty<string>() : c.Readings
            })),
            ResponseExcerpt = Truncate(sanitizedResponse, ResponseExcerptMaxChars),
            EvidenceExcerpt = Truncate(evidence, EvidenceExcerptMaxChars),
            EmptyDataDespiteSuccess = emptyDataDespiteSuccess
        };
    }

    // The lesson text is deliberately structural — kinds and counts, never the claim values
    // themselves: lessons are agent-wide, so claim values would leak user data, and Honest-Lying
    // style self-diagnosed causes are exactly what must not be stored.
    private async Task TriggerLessonIfRepeatedAsync(
        Guid agentId,
        LLMContext context,
        AnswerGroundingFinding finding,
        IReadOnlyList<AnswerClaim> uncovered,
        CancellationToken cancellationToken)
    {
        if (agentId == AnswerGroundingSentinel.AgentId)
        {
            return;
        }

        if (!string.Equals(_options.Mode, AnswerGroundingModes.Active, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var occurrences = await _repository.CountFindingsAsync(
            agentId, finding.ScopeKey, finding.PrimaryClaimKind, finding.Tier, cancellationToken);
        if (occurrences < LessonRepeatThreshold)
        {
            return;
        }

        var kinds = uncovered
            .GroupBy(c => c.Kind)
            .Select(g => $"{g.Count()} {g.Key.ToString().ToLowerInvariant()} value(s)")
            .ToList();
        var whatWentWrong =
            $"The answer stated {string.Join(" and ", kinds)} that no tool result, user message or " +
            $"context source contained. This is occurrence {occurrences} for this scope. " +
            "The missing data was simply not available in this turn.";

        _backgroundTasks.TriggerReflection(new TurnReflectionRequest(
            agentId,
            ReflectionTriggers.UncoveredClaim,
            context.Message,
            whatWentWrong,
            finding.ScopeKey,
            Guid.TryParse(context.UserId, out var userId) ? userId : null));
    }

    private async Task PersistCountersAsync(AnswerGroundingDailyCounter delta, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.IncrementDailyAsync(delta, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Answer grounding counter persistence failed for agent {AgentId}", delta.AgentId);
        }
    }

    private static string Truncate(string value, int maxChars)
        => value.Length <= maxChars ? value : value[..maxChars];
}
