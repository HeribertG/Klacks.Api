// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists a SkillSelectionTrajectory record per chat turn, capturing the candidate skills surfaced by
/// the knowledge index and the skill the LLM eventually chose. Privacy-preserving: stores only a short
/// SHA-256 hash plus a 120-char intent excerpt, never the full message.
/// </summary>
/// <param name="repository">Trajectory repository</param>
/// <param name="logger">Logger for telemetry warnings</param>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public class TrajectoryCaptureService : ITrajectoryCaptureService
{
    private const int HashPrefixLength = 16;
    private const int ExcerptMaxLength = 120;
    private const int CandidatesMax = 30;

    private readonly ISkillSelectionTrajectoryRepository _repository;
    private readonly ILogger<TrajectoryCaptureService> _logger;

    public TrajectoryCaptureService(
        ISkillSelectionTrajectoryRepository repository,
        ILogger<TrajectoryCaptureService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task CaptureAsync(Guid agentId, LLMContext context, string responseContent, List<LLMFunctionCall> allFunctionCalls)
    {
        try
        {
            var record = new SkillSelectionTrajectory
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                UserId = context.UserId,
                Locale = NormalizeLocale(context.Language),
                UserMessageHash = HashPrefix(context.Message),
                IntentExcerpt = ExtractExcerpt(context.Message),
                KnowledgeIndexCandidatesJson = SerializeCandidates(context.AvailableFunctions),
                LlmChosenSkill = allFunctionCalls.FirstOrDefault()?.FunctionName,
                WasExecuted = allFunctionCalls.Count > 0,
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

    private static string NormalizeLocale(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return "??";
        var trimmed = language.Trim();
        return trimmed.Length <= 8 ? trimmed : trimmed[..8];
    }

    private static string HashPrefix(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return new string('0', HashPrefixLength);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(bytes)[..HashPrefixLength];
    }

    private static string ExtractExcerpt(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return string.Empty;
        var trimmed = message.Trim();
        if (trimmed.Length <= ExcerptMaxLength) return trimmed;

        var sentenceEnd = trimmed.IndexOfAny(['.', '!', '?', '\n']);
        if (sentenceEnd > 0 && sentenceEnd <= ExcerptMaxLength) return trimmed[..sentenceEnd].Trim();

        return trimmed[..ExcerptMaxLength].Trim();
    }

    private static string SerializeCandidates(List<LLMFunction>? functions)
    {
        if (functions == null || functions.Count == 0) return "[]";
        var trimmed = functions.Count > CandidatesMax ? functions.GetRange(0, CandidatesMax) : functions;
        var payload = trimmed.Select(f => new { name = f.Name });
        return JsonSerializer.Serialize(payload);
    }
}
