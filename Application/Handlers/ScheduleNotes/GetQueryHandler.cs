// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one schedule note. A note owned by a client outside the caller's group visibility is answered
/// exactly like a note that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ScheduleNoteResource>, ScheduleNoteResource>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ScheduleNoteResource> Handle(GetQuery<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scheduleNote = await _scheduleNoteRepository.Get(request.Id);

            if (scheduleNote == null
                || !await _clientVisibilityGuard.IsVisibleAsync(scheduleNote.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"ScheduleNote with ID {request.Id} not found");
            }

            return _scheduleMapper.ToScheduleNoteResource(scheduleNote);
        }, nameof(Handle), new { request.Id });
    }
}
