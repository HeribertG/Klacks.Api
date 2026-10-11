// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a schedule note. A note whose stored owner or whose new owner is outside the caller's group
/// visibility is answered exactly like a note that does not exist; nothing is written. The AnalyseToken is taken
/// from the stored row, never from the payload, so a PUT cannot move a note between the main plan and a scenario.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client(s) and the stored row's scenario</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ScheduleNoteResource>, ScheduleNoteResource?>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleNoteResource?> Handle(PutCommand<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingScheduleNote = await _scheduleNoteRepository.GetNoTracking(request.Resource.Id);
            if (existingScheduleNote == null)
            {
                return null;
            }

            var scheduleNote = _scheduleMapper.ToScheduleNoteEntity(request.Resource);
            scheduleNote.AnalyseToken = existingScheduleNote.AnalyseToken;
            var ownerIds = new[] { existingScheduleNote.ClientId, scheduleNote.ClientId };
            if (!await _rowAccessGuard.AreAllVisibleAsync(ownerIds, scheduleNote.AnalyseToken, cancellationToken))
            {
                return null;
            }

            var updatedScheduleNote = await _scheduleNoteRepository.Put(scheduleNote);
            await _unitOfWork.CompleteAsync();

            return updatedScheduleNote != null ? _scheduleMapper.ToScheduleNoteResource(updatedScheduleNote) : null;
        }, "UpdateScheduleNote", new { request.Resource.Id });
    }
}
