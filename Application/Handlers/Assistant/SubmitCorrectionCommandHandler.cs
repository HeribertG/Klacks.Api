// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Marks a captured trajectory as user-corrected. Looks the trajectory up by user id + 16-char SHA-256
/// prefix of the user message (privacy-preserving: the original message is not stored).
/// </summary>

using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SubmitCorrectionCommandHandler : IRequestHandler<SubmitCorrectionCommand, SubmitCorrectionResult>
{
    private const int HashPrefixLength = 16;

    private static readonly HashSet<string> AllowedCorrectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        CorrectionTypes.WrongSkill,
        CorrectionTypes.WrongParam,
        CorrectionTypes.RepeatedRequest,
        CorrectionTypes.NoneNeeded
    };

    private readonly ISkillSelectionTrajectoryRepository _repository;
    private readonly ILLMBackgroundTaskService _backgroundTasks;
    private readonly ILogger<SubmitCorrectionCommandHandler> _logger;

    public SubmitCorrectionCommandHandler(
        ISkillSelectionTrajectoryRepository repository,
        ILLMBackgroundTaskService backgroundTasks,
        ILogger<SubmitCorrectionCommandHandler> logger)
    {
        _repository = repository;
        _backgroundTasks = backgroundTasks;
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

        var hash = HashPrefix(request.UserMessage);
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

        return new SubmitCorrectionResult(Found: true, TrajectoryId: trajectory.Id);
    }

    private static string HashPrefix(string message)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(bytes)[..HashPrefixLength];
    }
}
