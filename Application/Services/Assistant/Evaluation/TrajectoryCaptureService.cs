// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists a SkillSelectionTrajectory record per chat turn, capturing the candidate skills surfaced by
/// the knowledge index and the skill the LLM eventually chose. Privacy-preserving: stores only a short
/// SHA-256 hash plus a 120-char intent excerpt, never the full message. The hash comes from
/// MessageNormalizer, the same source the learning clusters and the correction endpoint use - this class
/// used to hash the raw message while the gap detector hashed a normalised one, so the two could never
/// recognise the same utterance.
/// </summary>
/// <param name="repository">Trajectory repository</param>
/// <param name="caseCollector">Learning collector, fed with the preceding turn once a negation corrects it</param>
/// <param name="logger">Logger for telemetry warnings</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public class TrajectoryCaptureService : ITrajectoryCaptureService
{
    private const int ExcerptMaxLength = 120;
    private const int CandidatesMax = 30;

    // How soon after the previous turn a negation/complaint ("nein", "falsch") is trusted as a
    // reactive correction of that turn rather than an unrelated later message that happens to
    // contain the same word.
    private static readonly TimeSpan ImplicitCorrectionWindow = TimeSpan.FromMinutes(2);

    private readonly ISkillSelectionTrajectoryRepository _repository;
    private readonly ISkillLearningCaseCollector _caseCollector;
    private readonly ILogger<TrajectoryCaptureService> _logger;

    public TrajectoryCaptureService(
        ISkillSelectionTrajectoryRepository repository,
        ISkillLearningCaseCollector caseCollector,
        ILogger<TrajectoryCaptureService> logger)
    {
        _repository = repository;
        _caseCollector = caseCollector;
        _logger = logger;
    }

    public async Task CaptureAsync(Guid agentId, LLMContext context, string responseContent, List<LLMFunctionCall> allFunctionCalls)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(context.UserId))
            {
                await MarkImplicitCorrectionIfApplicableAsync(agentId, context.UserId, context.Message);
            }

            var record = new SkillSelectionTrajectory
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                UserId = context.UserId,
                Locale = NormalizeLocale(context.Language),
                UserMessageHash = MessageNormalizer.Hash(context.Message),
                IntentExcerpt = MessageNormalizer.Excerpt(context.Message, ExcerptMaxLength),
                KnowledgeIndexCandidatesJson = SerializeCandidates(context.AvailableFunctions),
                LlmChosenSkill = allFunctionCalls.FirstOrDefault()?.FunctionName,
                WasExecuted = allFunctionCalls.Count > 0,
                HadMutationIntent = MutationIntentDetector.IsMutationIntent(context.Message),
                WasCorrected = false,
                CorrectionType = CorrectionTypes.None,
                LatencyMsTotal = 0,
                LatencyMsKnowledge = 0,
                LatencyMsLlm = 0,
                CreateTime = DateTime.UtcNow
            };

            await _repository.AddAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trajectory capture failed for agent {AgentId}", agentId);
        }
    }

    private async Task MarkImplicitCorrectionIfApplicableAsync(Guid agentId, string userId, string message)
    {
        if (!ImplicitCorrectionDetector.IsCorrectionSignal(message))
        {
            return;
        }

        var previous = await _repository.FindMostRecentByAgentAndUserAsync(agentId, userId);
        if (previous == null || previous.WasCorrected)
        {
            return;
        }

        if (DateTime.UtcNow - previous.CreateTime > ImplicitCorrectionWindow)
        {
            return;
        }

        previous.WasCorrected = true;
        previous.CorrectionType = CorrectionTypes.Implicit;
        previous.UpdateTime = DateTime.UtcNow;
        await _repository.UpdateAsync(previous);

        // The WasCorrected guard above is what keeps this to one case per corrected turn. The cluster key
        // is the stored hash of the preceding message, never a hash of its excerpt: for anything longer
        // than the excerpt limit the two differ and would split one wish across two clusters.
        await _caseCollector.CollectImplicitCorrectionAsync(new SkillLearningImplicitCorrection(
            agentId,
            previous.UserMessageHash,
            previous.IntentExcerpt,
            previous.UserId,
            previous.Locale,
            previous.LlmChosenSkill,
            previous.KnowledgeIndexCandidatesJson,
            previous.Id));
    }

    private static string NormalizeLocale(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return "??";
        var trimmed = language.Trim();
        return trimmed.Length <= 8 ? trimmed : trimmed[..8];
    }

    private static string SerializeCandidates(List<LLMFunction>? functions)
    {
        if (functions == null || functions.Count == 0) return "[]";
        var trimmed = functions.Count > CandidatesMax ? functions.GetRange(0, CandidatesMax) : functions;
        var payload = trimmed.Select(f => new { name = f.Name });
        return JsonSerializer.Serialize(payload);
    }
}
