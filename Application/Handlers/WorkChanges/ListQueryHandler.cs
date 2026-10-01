// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists WorkChanges. Only entries whose parent Work is owned by a client inside the caller's group visibility
/// are returned; an entry whose parent Work cannot be resolved is left out as well.
/// </summary>
/// <param name="workRepository">Resolves the owning client of each parent Work in one query</param>
/// <param name="clientVisibilityGuard">Filters the entries down to clients the calling user may see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<WorkChangeResource>, IEnumerable<WorkChangeResource>>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<WorkChangeResource>> Handle(ListQuery<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all WorkChanges");

        var allWorkChanges = await _workChangeRepository.List();
        var ownerByWorkId = (await _workRepository.GetByIdsAsync(allWorkChanges.Select(e => e.WorkId).Distinct()))
            .ToDictionary(w => w.Id, w => w.ClientId);
        var workChanges = await _clientVisibilityGuard.FilterVisibleAsync(
            allWorkChanges.Where(e => ownerByWorkId.ContainsKey(e.WorkId)).ToList(),
            e => ownerByWorkId[e.WorkId],
            cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} WorkChanges", workChanges.Count);
        return _scheduleMapper.ToWorkChangeResourceList(workChanges);
    }
}
