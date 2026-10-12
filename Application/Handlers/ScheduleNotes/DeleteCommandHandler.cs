// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes a schedule note. A note owned by a client outside the caller's group visibility is answered
/// exactly like a note that does not exist; nothing is deleted.
/// The stored row's day must not be sealed: IDayLockService answers a write on a sealed main-plan day with InvalidRequestException (400); scenario rows are exempt.
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

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ScheduleNoteResource>, ScheduleNoteResource?>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDayLockService _dayLockService;

    public DeleteCommandHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IDayLockService dayLockService,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _dayLockService = dayLockService;
    }

    public async Task<ScheduleNoteResource?> Handle(DeleteCommand<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        var existingScheduleNote = await _scheduleNoteRepository.Get(request.Id);
        if (existingScheduleNote == null
            || !await _rowAccessGuard.IsVisibleAsync(existingScheduleNote.ClientId, existingScheduleNote.AnalyseToken, cancellationToken))
        {
            return null;
        }

        var scheduleNoteResource = _scheduleMapper.ToScheduleNoteResource(existingScheduleNote);

        await _dayLockService.EnsureNotLockedAsync(
            existingScheduleNote.CurrentDate,
            existingScheduleNote.ClientId,
            existingScheduleNote.AnalyseToken,
            cancellationToken);

        await _scheduleNoteRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        return scheduleNoteResource;
    }
}
