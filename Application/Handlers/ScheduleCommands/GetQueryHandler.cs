// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving a single ScheduleCommand by ID. A command owned by a client outside the caller's
/// group visibility is answered exactly like a command that does not exist.
/// </summary>
/// <param name="request">Contains the ID to look up</param>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client(s) and the stored row's scenario</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScheduleCommands;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ScheduleCommandResource>, ScheduleCommandResource>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ScheduleCommandResource> Handle(GetQuery<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scheduleCommand = await _scheduleCommandRepository.Get(request.Id);

            if (scheduleCommand == null
                || !await _rowAccessGuard.IsVisibleAsync(scheduleCommand.ClientId, scheduleCommand.AnalyseToken, cancellationToken))
            {
                throw new KeyNotFoundException($"ScheduleCommand with ID {request.Id} not found");
            }

            return _scheduleMapper.ToScheduleCommandResource(scheduleCommand);
        }, nameof(Handle), new { request.Id });
    }
}
