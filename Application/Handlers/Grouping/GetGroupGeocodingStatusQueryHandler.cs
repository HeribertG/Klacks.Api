// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="GetGroupGeocodingStatusQuery"/>. Delegates to <see cref="IGroupRepository"/>
/// for the aggregate counts.
/// </summary>
/// <param name="groupRepository">Computes the group-geocoding progress counts.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Grouping;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Grouping;

public sealed class GetGroupGeocodingStatusQueryHandler
    : IRequestHandler<GetGroupGeocodingStatusQuery, GroupGeocodingStatus>
{
    private readonly IGroupRepository _groupRepository;

    public GetGroupGeocodingStatusQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<GroupGeocodingStatus> Handle(
        GetGroupGeocodingStatusQuery request, CancellationToken cancellationToken)
    {
        return await _groupRepository.GetGeocodingStatusAsync(cancellationToken);
    }
}
