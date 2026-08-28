// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the "wishes" section: what people asked for often enough to matter and Klacksy still cannot do.
/// Two statuses qualify. Ready means the threshold was reached and nothing has been learned from it yet -
/// before stage G2 exists that is every such cluster, and hiding them would leave the section permanently
/// empty. Unfulfillable means the loop tried and gave up. Status is part of the row so the card can tell
/// "not tried yet" from "tried and failed".
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
