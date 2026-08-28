// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Marks a captured trajectory as user-corrected. Looks the trajectory up by user id + the 16-char
/// MessageNormalizer hash of the user message (privacy-preserving: the original message is not stored).
/// Because that hash is normalised, a correction whose text differs from the captured turn only in
/// casing or surrounding whitespace now finds its trajectory instead of silently reporting "not found".
/// A correction naming the expected skill is also the only evidence the learning loop gets that is not a
/// refusal, so it is forwarded to the case collector.
/// </summary>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SubmitCorrectionCommandHandler : IRequestHandler<SubmitCorrectionCommand, SubmitCorrectionResult>
{
    private static readonly HashSet<string> AllowedCorrectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        CorrectionTypes.WrongSkill,
        CorrectionTypes.WrongParam,
        CorrectionTypes.RepeatedRequest,
        CorrectionTypes.NoneNeeded
    };

    private readonly ISkillSelectionTrajectoryRepository _repository;
    private readonly ILLMBackgroundTaskService _backgroundTasks;
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly ISkillLearningCaseCollector _caseCollector;
    private readonly ILogger<SubmitCorrectionCommandHandler> _logger;

    public SubmitCorrectionCommandHandler(
        ISkillSelectionTrajectoryRepository repository,
        ILLMBackgroundTaskService backgroundTasks,
        IAgentMemoryRepository agentMemoryRepository,
        ISkillLearningCaseCollector caseCollector,
        ILogger<SubmitCorrectionCommandHandler> logger)
    {
        _repository = repository;
        _backgroundTasks = backgroundTasks;
        _agentMemoryRepository = agentMemoryRepository;
        _caseCollector = caseCollector;
        _logger = logger;
    }

    public async Task<SubmitCorrectionResult> Handle(SubmitCorrectionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId must be provided.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            throw new ArgumentException("UserMessage must be provided.", nameof(request));
        }

        if (!AllowedCorrectionTypes.Contains(request.CorrectionType))
        {
            throw new ArgumentException($"Unknown correction type '{request.CorrectionType}'.", nameof(request));
        }

        var hash = MessageNormalizer.Hash(request.UserMessage);
        var trajectory = await _repository.FindMostRecentByUserAndHashAsync(request.UserId, hash, cancellationToken);

        if (trajectory == null)
        {
            _logger.LogInformation(
                "Correction received for user {UserId} but no matching trajectory was found (hash {Hash})",
                request.UserId, hash);
            return new SubmitCorrectionResult(Found: false, TrajectoryId: null);
        }

        trajectory.WasCorrected = true;
        trajectory.CorrectionType = request.CorrectionType.ToLowerInvariant();
        trajectory.UpdateTime = DateTime.UtcNow;

        await _repository.UpdateAsync(trajectory, cancellationToken);

        _logger.LogInformation(
            "Correction applied to trajectory {TrajectoryId}: type={Type}",
            trajectory.Id, trajectory.CorrectionType);

        // A user correction is the strongest evidence a turn went wrong, so it feeds the reflection.
        // NoneNeeded says the turn was fine after all and must not produce a lesson.
        if (string.Equals(trajectory.CorrectionType, CorrectionTypes.NoneNeeded, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(trajectory.LlmChosenSkill))
        {
            await RevokeLatestUncoveredClaimLessonAsync(trajectory.AgentId, trajectory.LlmChosenSkill!, cancellationToken);
        }

        if (!string.Equals(trajectory.CorrectionType, CorrectionTypes.NoneNeeded, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(trajectory.LlmChosenSkill))
        {
            _backgroundTasks.TriggerReflection(new TurnReflectionRequest(
                trajectory.AgentId,
                ReflectionTriggers.UserCorrection,
                request.UserMessage,
                $"The user corrected this turn as '{trajectory.CorrectionType}'. " +
                $"The capability chosen was {trajectory.LlmChosenSkill}.",
                trajectory.LlmChosenSkill,
                Guid.TryParse(request.UserId, out var correctingUserId) ? correctingUserId : null));
        }

        await CollectLearningCaseAsync(request, trajectory, cancellationToken);

        return new SubmitCorrectionResult(Found: true, TrajectoryId: trajectory.Id);
    }

    // Only the correction types that say something about ROUTING become learning cases: a wrong
    // parameter and a repeated request are about the turn, not about which capability was missing. The
    // decision comes from the explicit map, not from a name match against the signal list - that list
    // also holds refusal, which no correction type ever carries.
    private async Task CollectLearningCaseAsync(
        SubmitCorrectionCommand request, SkillSelectionTrajectory trajectory, CancellationToken cancellationToken)
    {
        var signal = CorrectionTypeLearningSignals.Resolve(trajectory.CorrectionType);
        if (signal == null)
        {
            return;
        }

        await _caseCollector.CollectCorrectionAsync(
            new SkillLearningCorrection(
                trajectory.AgentId,
                request.UserMessage,
                signal,
                request.UserId,
                trajectory.Locale,
                trajectory.LlmChosenSkill,
                request.ExpectedSkill,
                trajectory.Id),
            cancellationToken);
    }

    // NoneNeeded says the turn was fine after all: a coverage lesson previously drawn for this
    // skill is thereby contradicted by the user and must not keep poisoning future turns.
    private async Task RevokeLatestUncoveredClaimLessonAsync(Guid agentId, string scopeKey, CancellationToken cancellationToken)
    {
        var lessons = await _agentMemoryRepository.GetByCategoryAndKeysAsync(
            agentId, MemoryCategories.Reflection, new[] { scopeKey }, limit: 10, cancellationToken);
        var latest = lessons
            .Where(l => string.Equals(l.SourceRef, ReflectionTriggers.UncoveredClaim, StringComparison.Ordinal))
            .OrderByDescending(l => l.CreateTime)
            .FirstOrDefault();
        if (latest == null)
        {
            return;
        }

        await _agentMemoryRepository.DeleteAsync(latest.Id, cancellationToken);
        _logger.LogInformation("Revoked uncovered-claim lesson {MemoryId} for '{ScopeKey}' after a NoneNeeded correction", latest.Id, scopeKey);
    }
}
