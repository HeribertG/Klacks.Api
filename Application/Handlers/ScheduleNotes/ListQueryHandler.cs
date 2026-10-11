// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the schedule notes of the main plan. Notes owned by clients outside the caller's group visibility are left
/// out, and so are scenario notes (AnalyseToken set): this list takes no token, so it must not enumerate the rows of
/// scenarios the caller may not see.
/// </summary>
/// <param name="clientVisibilityGuard">Filters the notes down to clients the calling user may see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<ScheduleNoteResource>, IEnumerable<ScheduleNoteResource>>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<ScheduleNoteResource>> Handle(ListQuery<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        var scheduleNotes = (await _scheduleNoteRepository.List()).Where(n => n.AnalyseToken == null).ToList();

        var visible = await _clientVisibilityGuard.FilterVisibleAsync(
            scheduleNotes, n => n.ClientId, cancellationToken);

        return _scheduleMapper.ToScheduleNoteResourceList(visible);
    }
}
