// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns the real shifts available to a client through their group memberships. The group tree of a client spans every
/// sister group, so the shifts are limited to those the caller may see. A client outside the caller's group visibility is
/// answered exactly like a client that does not exist: with an empty list.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the client</param>
/// <param name="shiftVisibilityGuard">Leaves out the shifts the calling user may not see</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ClientShiftPreferences;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientShiftPreferences;

public class GetAvailableShiftsQueryHandler : BaseHandler,
    IRequestHandler<GetAvailableShiftsQuery, List<AvailableShiftResource>>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public GetAvailableShiftsQueryHandler(
        IGroupItemRepository groupItemRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<GetAvailableShiftsQueryHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<List<AvailableShiftResource>> Handle(
        GetAvailableShiftsQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _clientVisibilityGuard.IsVisibleAsync(request.ClientId, cancellationToken))
                return [];

            var treeGroupIds = await _groupItemRepository
                .GetGroupTreeIdsForClientAsync(request.ClientId, cancellationToken);

            if (treeGroupIds.Count == 0)
                return [];

            var shifts = await _groupItemRepository.GetLinkedShiftNamesAsync(treeGroupIds, cancellationToken);
            var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(shifts.Select(s => s.Id).ToList(), cancellationToken);

            return shifts.Where(s => visibleIds.Contains(s.Id)).Select(s => new AvailableShiftResource
            {
                Id = s.Id,
                Name = s.Name,
                Abbreviation = s.Abbreviation,
            }).ToList();
        },
        "getting available shifts for client",
        new { request.ClientId });
    }
}
