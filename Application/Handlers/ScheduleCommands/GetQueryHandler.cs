// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving a single ScheduleCommand by ID.
/// </summary>
/// <param name="request">Contains the ID to look up</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScheduleCommands;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ScheduleCommandResource>, ScheduleCommandResource>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ScheduleCommandResource> Handle(GetQuery<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scheduleCommand = await _scheduleCommandRepository.Get(request.Id);

            if (scheduleCommand == null)
            {
                throw new KeyNotFoundException($"ScheduleCommand with ID {request.Id} not found");
            }

            return _scheduleMapper.ToScheduleCommandResource(scheduleCommand);
        }, nameof(Handle), new { request.Id });
    }
}
