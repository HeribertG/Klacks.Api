// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Self-monitoring probe against a silently dead grounding pipeline: runs a synthetic turn whose
/// answer contains an identifier no context source covers through the real evaluator, then
/// verifies via the repository that the tier-1 finding was actually persisted. Zero findings over
/// time are only trustworthy while this probe keeps proving the pipeline can still fire; a failed
/// probe is logged as an error so it stays visible under Warning-level production logging.
/// </summary>
/// <param name="options">Bound Assistant:AnswerGroundingMode configuration (Off skips the probe).</param>
/// <param name="evaluator">The real evaluator whose full pipeline the probe exercises.</param>
/// <param name="repository">Counts persisted sentinel findings before and after the probe.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Grounding;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Grounding;

public class AnswerGroundingSentinelProbe : IAnswerGroundingSentinelProbe
{
    private const string ProbeUuid = "9d1a5e00-1111-4111-8111-111111111111";
    private const string ProbeMessage = "Sentinel probe: verify that the grounding evaluator still detects an uncovered identifier.";
    private const string ProbeLanguage = "en";

    private readonly AnswerGroundingOptions _options;
    private readonly IAnswerGroundingEvaluator _evaluator;
    private readonly IAnswerGroundingRepository _repository;
    private readonly ILogger<AnswerGroundingSentinelProbe> _logger;

    public AnswerGroundingSentinelProbe(
        AnswerGroundingOptions options,
        IAnswerGroundingEvaluator evaluator,
        IAnswerGroundingRepository repository,
        ILogger<AnswerGroundingSentinelProbe> logger)
    {
        _options = options;
        _evaluator = evaluator;
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            return true;
        }

        var before = await CountSentinelFindingsAsync(cancellationToken);

        var context = new LLMContext
        {
            Message = ProbeMessage,
            Language = ProbeLanguage,
            UserId = string.Empty
        };

        await _evaluator.EvaluateAsync(
            AnswerGroundingSentinel.AgentId,
            context,
            $"Sentinel probe answer: entity {ProbeUuid} was updated.",
            Array.Empty<LLMFunctionCall>(),
            cancellationToken);

        var after = await CountSentinelFindingsAsync(cancellationToken);
        if (after > before)
        {
            _logger.LogInformation(
                "Answer grounding sentinel probe produced its finding (total {Count})", after);
            return true;
        }

        _logger.LogError(
            "Answer grounding sentinel probe produced NO finding — the grounding pipeline is broken or silently dead (findings before: {Before}, after: {After})",
            before,
            after);
        return false;
    }

    private Task<int> CountSentinelFindingsAsync(CancellationToken cancellationToken)
        => _repository.CountFindingsAsync(
            AnswerGroundingSentinel.AgentId,
            AnswerGroundingEvaluator.NoToolScopeKey,
            nameof(AnswerClaimKind.Uuid),
            AnswerGroundingEvaluator.UuidTier,
            cancellationToken);
}
