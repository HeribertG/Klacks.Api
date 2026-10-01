// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads a single WorkChange by id. A change whose parent Work is owned by a client outside the caller's
/// group visibility is answered exactly like a change that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<WorkChangeResource>, WorkChangeResource>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IWorkChangeRepository workChangeRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<WorkChangeResource> Handle(GetQuery<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var workChange = await _workChangeRepository.Get(request.Id);

            if (workChange == null
                || (workChange.Work != null
                    && !await _clientVisibilityGuard.IsVisibleAsync(workChange.Work.ClientId, cancellationToken)))
            {
                throw new KeyNotFoundException($"WorkChange with ID {request.Id} not found");
            }

            return _scheduleMapper.ToWorkChangeResource(workChange);
        }, nameof(Handle), new { request.Id });
    }
}
