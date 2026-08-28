// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Discards a wish an administrator judged not worth serving. Dismissed is terminal: the collector stops
/// counting the cluster, so the same utterance never resurfaces the wish. Retention soft-deletes it later.
/// </summary>
/// <param name="clusterRepository">Cluster store</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class DismissUnfulfillableWishCommandHandler
    : IRequestHandler<DismissUnfulfillableWishCommand, LearningMutationResult>
{
    private readonly ISkillLearningClusterRepository _clusterRepository;

    public DismissUnfulfillableWishCommandHandler(ISkillLearningClusterRepository clusterRepository)
    {
        _clusterRepository = clusterRepository;
    }

    public async Task<LearningMutationResult> Handle(
        DismissUnfulfillableWishCommand request, CancellationToken cancellationToken)
    {
        var cluster = await _clusterRepository.GetByIdAsync(request.Id, cancellationToken);
        if (cluster == null)
        {
            return LearningMutationResult.NotFound();
        }

        if (!SkillLearningStateMachine.IsLegalTransition(
                cluster.Status, SkillLearningClusterStatuses.Dismissed))
        {
            return LearningMutationResult.Invalid(
                $"A cluster in status '{cluster.Status}' cannot be dismissed.");
        }

        var transitioned = await _clusterRepository.TryTransitionAsync(
            cluster.Id, cluster.Status, SkillLearningClusterStatuses.Dismissed, cancellationToken);

        return transitioned ? LearningMutationResult.Success() : LearningMutationResult.NotFound();
    }
}
