// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Records a thumbs-up on the turn the given utterance produced. Finds the turn exactly the way the
/// correction endpoint does - most recent trajectory of this user under the MessageNormalizer hash - so
/// praise and complaint can never disagree about which turn was meant.
/// Marking is idempotent and deliberately one-way: a turn already flagged helpful is reported as found
/// and left alone, and nothing here can clear the flag or touch WasCorrected. A user who first praises
/// and then corrects keeps both statements, which is the truth the fitness quote should see.
/// An unknown utterance is not an error: trajectory capture is fire-and-forget and may have lost the
/// turn, and the client would have nothing useful to do with a 404 anyway.
/// </summary>
/// <param name="repository">Trajectory store, self-committing</param>
/// <param name="logger">Reports the miss, which is the only interesting outcome</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SubmitHelpfulFeedbackCommandHandler
    : IRequestHandler<SubmitHelpfulFeedbackCommand, SubmitHelpfulFeedbackResult>
{
    private readonly ISkillSelectionTrajectoryRepository _repository;
    private readonly ILogger<SubmitHelpfulFeedbackCommandHandler> _logger;

    public SubmitHelpfulFeedbackCommandHandler(
        ISkillSelectionTrajectoryRepository repository,
        ILogger<SubmitHelpfulFeedbackCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SubmitHelpfulFeedbackResult> Handle(
        SubmitHelpfulFeedbackCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            throw new ArgumentException("UserMessage is required.", nameof(request));
        }

        var hash = MessageNormalizer.Hash(request.UserMessage);
        var trajectory = await _repository.FindMostRecentByUserAndHashAsync(
            request.UserId, hash, cancellationToken);

        if (trajectory == null)
        {
            _logger.LogInformation(
                "Helpful feedback found no trajectory for hash {Hash}; the turn was not captured", hash);
            return new SubmitHelpfulFeedbackResult(Found: false, TrajectoryId: null);
        }

        if (trajectory.Helpful == true)
        {
            return new SubmitHelpfulFeedbackResult(Found: true, TrajectoryId: trajectory.Id);
        }

        trajectory.Helpful = true;
        trajectory.UpdateTime = DateTime.UtcNow;
        await _repository.UpdateAsync(trajectory, cancellationToken);

        return new SubmitHelpfulFeedbackResult(Found: true, TrajectoryId: trajectory.Id);
    }
}
