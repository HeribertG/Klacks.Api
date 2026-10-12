// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for updating an existing ScheduleCommand. A command whose stored owner or whose new owner is
/// outside the caller's group visibility is answered exactly like a command that does not exist; nothing is
/// written. The AnalyseToken is taken from the stored row, never from the payload, so a PUT cannot move a command
/// between the main plan and a scenario.
/// Both the stored and the new day must not be sealed (moving a row out of or into a sealed day is refused): IDayLockService answers a write on a sealed main-plan day with InvalidRequestException (400); scenario rows are exempt.
/// </summary>
/// <param name="request">Contains the updated ScheduleCommandResource</param>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client(s) and the stored row's scenario</param>
/// <param name="dayLockService">Refuses the write when the affected (date, client) pair lies on a sealed day</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleCommands;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ScheduleCommandResource>, ScheduleCommandResource?>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDayLockService _dayLockService;

    public PutCommandHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IDayLockService dayLockService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _dayLockService = dayLockService;
    }

    public async Task<ScheduleCommandResource?> Handle(PutCommand<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _scheduleCommandRepository.GetNoTracking(request.Resource.Id);
            if (existing == null)
            {
                return null;
            }

            var scheduleCommand = _scheduleMapper.ToScheduleCommandEntity(request.Resource);
            scheduleCommand.AnalyseToken = existing.AnalyseToken;

            if (!await _rowAccessGuard.AreAllVisibleAsync([existing.ClientId, scheduleCommand.ClientId], scheduleCommand.AnalyseToken, cancellationToken))
            {
                return null;
            }

            await _dayLockService.EnsureNotLockedAsync(
                scheduleCommand.CurrentDate,
                scheduleCommand.ClientId,
                scheduleCommand.AnalyseToken,
                cancellationToken);

            if (existing.CurrentDate != scheduleCommand.CurrentDate || existing.ClientId != scheduleCommand.ClientId)
            {
                await _dayLockService.EnsureNotLockedAsync(
                    existing.CurrentDate,
                    existing.ClientId,
                    existing.AnalyseToken,
                    cancellationToken);
            }

            var updated = await _scheduleCommandRepository.Put(scheduleCommand);
            await _unitOfWork.CompleteAsync();

            return updated != null ? _scheduleMapper.ToScheduleCommandResource(updated) : null;
        }, "UpdateScheduleCommand", new { request.Resource.Id });
    }
}
