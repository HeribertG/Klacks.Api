// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for listing all ScheduleCommands. Commands owned by clients outside the caller's group visibility
/// are left out.
/// </summary>
/// <param name="clientVisibilityGuard">Filters the commands down to clients the calling user may see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleCommands;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<ScheduleCommandResource>, IEnumerable<ScheduleCommandResource>>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<ScheduleCommandResource>> Handle(ListQuery<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        var scheduleCommands = await _scheduleCommandRepository.List();
        var visible = await _clientVisibilityGuard.FilterVisibleAsync(
            scheduleCommands, c => c.ClientId, cancellationToken);

        return _scheduleMapper.ToScheduleCommandResourceList(visible);
    }
}
