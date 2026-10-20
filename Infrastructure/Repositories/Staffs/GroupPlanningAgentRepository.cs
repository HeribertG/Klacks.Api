// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the planning agents of a selected group through the schedule view's own client scope and
/// group filter, so planning skills see a sub-group's members (and its descendants) exactly as the
/// schedule does — unlike the root-keyed ClientRepository.GetActiveClientsWithAddressesForGroupsAsync.
/// </summary>
/// <param name="context">Database context</param>
/// <param name="groupFilterService">Group subtree + visibility filter shared with the schedule client list</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services.Clients;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class GroupPlanningAgentRepository : IGroupPlanningAgentRepository
{
    private readonly DataBaseContext _context;
    private readonly IClientGroupFilterService _groupFilterService;

    public GroupPlanningAgentRepository(DataBaseContext context, IClientGroupFilterService groupFilterService)
    {
        _context = context;
        _groupFilterService = groupFilterService;
    }

    public async Task<List<Guid>> GetAgentIdsAsync(
        Guid groupId, DateOnly periodFrom, DateOnly periodUntil, CancellationToken cancellationToken = default)
    {
        var scoped = ScheduleClientScope.ActiveInPeriod(_context.Client.AsNoTracking(), periodFrom, periodUntil);
        var inGroup = await _groupFilterService.FilterClientsByGroupId(groupId, scoped);

        return await inGroup
            .Select(c => c.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PlanningAgentEmployment>> GetAgentEmploymentsAsync(
        Guid groupId, DateOnly periodFrom, DateOnly periodUntil, CancellationToken cancellationToken = default)
    {
        var scoped = ScheduleClientScope.ActiveInPeriod(_context.Client.AsNoTracking(), periodFrom, periodUntil);
        var inGroup = await _groupFilterService.FilterClientsByGroupId(groupId, scoped);

        var rows = await inGroup
            .Select(c => new { c.Id, c.Membership!.ValidFrom, c.Membership.ValidUntil })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new PlanningAgentEmployment(
                row.Id,
                new MembershipWindow(
                    DateOnly.FromDateTime(row.ValidFrom),
                    row.ValidUntil.HasValue ? DateOnly.FromDateTime(row.ValidUntil.Value) : null)))
            .ToList();
    }
}
