// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<ScheduleNoteResource>, IEnumerable<ScheduleNoteResource>>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<ScheduleNoteResource>> Handle(ListQuery<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        var scheduleNotes = await _scheduleNoteRepository.List();

        return _scheduleMapper.ToScheduleNoteResourceList(scheduleNotes);
    }
}
