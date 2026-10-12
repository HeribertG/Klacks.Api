// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a schedule note. A note whose stored owner or whose new owner is outside the caller's group
/// visibility is answered exactly like a note that does not exist; nothing is written. The AnalyseToken is taken
/// from the stored row, never from the payload, so a PUT cannot move a note between the main plan and a scenario.
/// Both the stored and the new day must not be sealed (moving a row out of or into a sealed day is refused): IDayLockService answers a write on a sealed main-plan day with InvalidRequestException (400); scenario rows are exempt.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client(s) and the stored row's scenario</param>
/// <param name="dayLockService">Refuses the write when the affected (date, client) pair lies on a sealed day</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ScheduleNoteResource>, ScheduleNoteResource?>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDayLockService _dayLockService;

    public PutCommandHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IDayLockService dayLockService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _dayLockService = dayLockService;
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

            await _dayLockService.EnsureNotLockedAsync(
                scheduleNote.CurrentDate,
                scheduleNote.ClientId,
                scheduleNote.AnalyseToken,
                cancellationToken);

            if (existingScheduleNote.CurrentDate != scheduleNote.CurrentDate || existingScheduleNote.ClientId != scheduleNote.ClientId)
            {
                await _dayLockService.EnsureNotLockedAsync(
                    existingScheduleNote.CurrentDate,
                    existingScheduleNote.ClientId,
                    existingScheduleNote.AnalyseToken,
                    cancellationToken);
            }

            var updatedScheduleNote = await _scheduleNoteRepository.Put(scheduleNote);
            await _unitOfWork.CompleteAsync();

            return updatedScheduleNote != null ? _scheduleMapper.ToScheduleNoteResource(updatedScheduleNote) : null;
        }, "UpdateScheduleNote", new { request.Resource.Id });
    }
}
