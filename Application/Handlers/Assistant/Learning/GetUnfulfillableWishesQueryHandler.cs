// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the "wishes" section: what people asked for often enough to matter and Klacksy still cannot do.
/// Two statuses qualify. Ready means the threshold was reached and the loop has not got to it yet, or its
/// last round failed and it will try again; Unfulfillable means it tried and gave up. Status is part of
/// the row so the card can tell "waiting" from "given up on". The weekly digest counts only the second of
/// the two - a wish the next run may still close is not a wish nobody can serve.
/// </summary>
/// <param name="clusterRepository">Cluster store</param>

using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Application.Queries.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class GetUnfulfillableWishesQueryHandler
    : BaseHandler, IRequestHandler<GetUnfulfillableWishesQuery, IReadOnlyList<UnfulfillableWishDto>>
{
    public static readonly IReadOnlyList<string> WishStatuses =
    [
        SkillLearningClusterStatuses.Ready,
        SkillLearningClusterStatuses.Unfulfillable
    ];

    private readonly ISkillLearningClusterRepository _clusterRepository;

    public GetUnfulfillableWishesQueryHandler(
        ISkillLearningClusterRepository clusterRepository,
        ILogger<GetUnfulfillableWishesQueryHandler> logger)
        : base(logger)
    {
        _clusterRepository = clusterRepository;
    }

    public async Task<IReadOnlyList<UnfulfillableWishDto>> Handle(
        GetUnfulfillableWishesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            async () =>
            {
                var clusters = await _clusterRepository.ListByStatusAsync(
                    WishStatuses, request.Limit, cancellationToken);

                return (IReadOnlyList<UnfulfillableWishDto>)clusters
                    .Select(cluster => new UnfulfillableWishDto(
                        cluster.Id,
                        cluster.IntentExcerpt,
                        cluster.Locale,
                        cluster.Status,
                        cluster.OccurrenceCount,
                        cluster.DistinctUserCount,
                        cluster.FirstSeenAtUtc,
                        cluster.LastSeenAtUtc,
                        cluster.LastError))
                    .ToList();
            },
            "get unfulfillable wishes",
            new { request.Limit });
    }
}
