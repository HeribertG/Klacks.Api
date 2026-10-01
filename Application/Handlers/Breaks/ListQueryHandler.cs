// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists all breaks (absence entries). Breaks owned by clients outside the caller's group visibility are
/// left out, because the absence type is personal (health) data.
/// </summary>
/// <param name="clientVisibilityGuard">Filters the breaks down to clients the calling user may see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<BreakResource>, IEnumerable<BreakResource>>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IBreakRepository breakRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<BreakResource>> Handle(ListQuery<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = await _breakRepository.List();
            var visible = await _clientVisibilityGuard.FilterVisibleAsync(entities, b => b.ClientId, cancellationToken);
            return visible.Select(_scheduleMapper.ToBreakResource).ToList();
        }, "ListBreaks");
    }
}
