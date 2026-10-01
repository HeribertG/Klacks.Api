// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads a single Work by id. A Work owned by a client outside the caller's group visibility is answered
/// exactly like a Work that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<WorkResource>, WorkResource>
{
    private readonly IWorkRepository _workRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<WorkResource> Handle(GetQuery<WorkResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.Id);

            if (work == null || !await _clientVisibilityGuard.IsVisibleAsync(work.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found");
            }

            return _scheduleMapper.ToWorkResource(work);
        }, nameof(Handle), new { request.Id });
    }
}
