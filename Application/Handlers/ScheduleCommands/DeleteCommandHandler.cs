// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for soft-deleting a ScheduleCommand. A command owned by a client outside the caller's group
/// visibility is answered exactly like a command that does not exist; nothing is deleted.
/// The stored row's day must not be sealed: IDayLockService answers a write on a sealed main-plan day with InvalidRequestException (400); scenario rows are exempt.
/// </summary>
/// <param name="request">Contains the ID of the command to delete</param>
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

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ScheduleCommandResource>, ScheduleCommandResource?>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDayLockService _dayLockService;

    public DeleteCommandHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IDayLockService dayLockService,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _dayLockService = dayLockService;
    }

    public async Task<ScheduleCommandResource?> Handle(DeleteCommand<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        var existing = await _scheduleCommandRepository.Get(request.Id);
        if (existing == null || !await _rowAccessGuard.IsVisibleAsync(existing.ClientId, existing.AnalyseToken, cancellationToken))
        {
            return null;
        }

        var resource = _scheduleMapper.ToScheduleCommandResource(existing);

        await _dayLockService.EnsureNotLockedAsync(
            existing.CurrentDate,
            existing.ClientId,
            existing.AnalyseToken,
            cancellationToken);

        await _scheduleCommandRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        return resource;
    }
}
