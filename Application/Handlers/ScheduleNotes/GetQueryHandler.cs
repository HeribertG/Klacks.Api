// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one schedule note. A note owned by a client outside the caller's group visibility is answered
/// exactly like a note that does not exist.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client(s) and the stored row's scenario</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ScheduleNoteResource>, ScheduleNoteResource>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ScheduleNoteResource> Handle(GetQuery<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scheduleNote = await _scheduleNoteRepository.Get(request.Id);

            if (scheduleNote == null
                || !await _rowAccessGuard.IsVisibleAsync(scheduleNote.ClientId, scheduleNote.AnalyseToken, cancellationToken))
            {
                throw new KeyNotFoundException($"ScheduleNote with ID {request.Id} not found");
            }

            return _scheduleMapper.ToScheduleNoteResource(scheduleNote);
        }, nameof(Handle), new { request.Id });
    }
}
