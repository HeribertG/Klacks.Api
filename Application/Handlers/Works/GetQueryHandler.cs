// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<WorkResource>, WorkResource>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(IWorkRepository workRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<WorkResource> Handle(GetQuery<WorkResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.Id);

            if (work == null)
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found");
            }

            return _scheduleMapper.ToWorkResource(work);
        }, nameof(Handle), new { request.Id });
    }
}
