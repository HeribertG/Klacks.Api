// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns all shifts available to a client through their group memberships.
/// </summary>
using Klacks.Api.Application.Queries.ClientShiftPreferences;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.ClientShiftPreferences;

public class GetAvailableShiftsQueryHandler : BaseHandler,
    IRequestHandler<GetAvailableShiftsQuery, List<AvailableShiftResource>>
{
    private readonly IGroupItemRepository _groupItemRepository;

    public GetAvailableShiftsQueryHandler(
        IGroupItemRepository groupItemRepository,
        ILogger<GetAvailableShiftsQueryHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
    }

    public async Task<List<AvailableShiftResource>> Handle(
        GetAvailableShiftsQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var treeGroupIds = await _groupItemRepository
                .GetGroupTreeIdsForClientAsync(request.ClientId, cancellationToken);

            if (treeGroupIds.Count == 0)
                return [];

            var shifts = await _groupItemRepository.GetQuery()
                .Include(gi => gi.Shift)
                .Where(gi => treeGroupIds.Contains(gi.GroupId) && gi.ShiftId != null)
                .Where(gi => gi.Shift != null)
                .Select(gi => new { gi.Shift!.Id, gi.Shift.Name, gi.Shift.Abbreviation })
                .Distinct()
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return shifts.Select(s => new AvailableShiftResource
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
