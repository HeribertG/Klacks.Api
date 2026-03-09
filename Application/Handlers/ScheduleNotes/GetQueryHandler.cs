// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ScheduleNoteResource>, ScheduleNoteResource>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ScheduleNoteResource> Handle(GetQuery<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scheduleNote = await _scheduleNoteRepository.Get(request.Id);

            if (scheduleNote == null)
            {
                throw new KeyNotFoundException($"ScheduleNote with ID {request.Id} not found");
            }

            return _scheduleMapper.ToScheduleNoteResource(scheduleNote);
        }, nameof(Handle), new { request.Id });
    }
}
