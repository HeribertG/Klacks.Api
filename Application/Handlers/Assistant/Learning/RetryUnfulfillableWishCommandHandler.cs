// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Hands a wish the loop gave up on back to the learning loop. The state machine has always allowed
/// unfulfillable to return to ready - it is what makes unfulfillable a resting place rather than a
/// terminal status - but until now nothing could perform that move, so the documented way back was
/// unreachable in practice. Reopening also clears the attempt budget and the recorded error: the reason
/// to reopen is that something changed (a new skill, a corrected description), and carrying a spent budget
/// forward would let the wish fall straight back out after a single new attempt.
/// </summary>
/// <param name="clusterRepository">Cluster store</param>
/// <param name="logger">Records who was handed back to the loop</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class RetryUnfulfillableWishCommandHandler
    : IRequestHandler<RetryUnfulfillableWishCommand, LearningMutationResult>
{
    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly ILogger<RetryUnfulfillableWishCommandHandler> _logger;

    public RetryUnfulfillableWishCommandHandler(
        ISkillLearningClusterRepository clusterRepository,
        ILogger<RetryUnfulfillableWishCommandHandler> logger)
    {
        _clusterRepository = clusterRepository;
        _logger = logger;
    }

    public async Task<LearningMutationResult> Handle(
        RetryUnfulfillableWishCommand request, CancellationToken cancellationToken)
    {
        var cluster = await _clusterRepository.GetByIdAsync(request.Id, cancellationToken);
        if (cluster == null)
        {
            return LearningMutationResult.NotFound();
        }

        if (!string.Equals(
                cluster.Status, SkillLearningClusterStatuses.Unfulfillable, StringComparison.Ordinal))
        {
            return LearningMutationResult.Invalid(
                $"Only a wish in status '{SkillLearningClusterStatuses.Unfulfillable}' can be handed back "
                    + $"to the learning loop; this one is '{cluster.Status}'.");
        }

        var reopened = await _clusterRepository.TryRetryUnfulfillableAsync(request.Id, cancellationToken);
        if (!reopened)
        {
            return LearningMutationResult.NotFound();
        }

        _logger.LogInformation("Unfulfillable wish {ClusterId} was handed back to the learning loop", request.Id);

        return LearningMutationResult.Success();
    }
}
