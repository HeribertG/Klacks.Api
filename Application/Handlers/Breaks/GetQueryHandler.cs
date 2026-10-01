// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one break (absence entry). A break owned by a client outside the caller's group visibility is
/// answered exactly like a break that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<BreakResource>, BreakResource>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IBreakRepository breakRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<BreakResource> Handle(GetQuery<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = await _breakRepository.Get(request.Id);

            if (entity == null || !await _clientVisibilityGuard.IsVisibleAsync(entity.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Break with ID {request.Id} not found");
            }

            return _scheduleMapper.ToBreakResource(entity);
        }, "GetBreak", new { request.Id });
    }
}
