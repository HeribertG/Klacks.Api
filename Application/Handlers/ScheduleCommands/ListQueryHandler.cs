// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for listing all ScheduleCommands.
/// </summary>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleCommands;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<ScheduleCommandResource>, IEnumerable<ScheduleCommandResource>>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<ScheduleCommandResource>> Handle(ListQuery<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        var scheduleCommands = await _scheduleCommandRepository.List();

        return _scheduleMapper.ToScheduleCommandResourceList(scheduleCommands);
    }
}
