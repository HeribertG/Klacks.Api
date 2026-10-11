// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for updating an existing ScheduleCommand. A command whose stored owner or whose new owner is
/// outside the caller's group visibility is answered exactly like a command that does not exist; nothing is
/// written. The AnalyseToken is taken from the stored row, never from the payload, so a PUT cannot move a command
/// between the main plan and a scenario.
/// </summary>
/// <param name="request">Contains the updated ScheduleCommandResource</param>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client(s) and the stored row's scenario</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleCommands;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ScheduleCommandResource>, ScheduleCommandResource?>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
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

            var updated = await _scheduleCommandRepository.Put(scheduleCommand);
            await _unitOfWork.CompleteAsync();

            return updated != null ? _scheduleMapper.ToScheduleCommandResource(updated) : null;
        }, "UpdateScheduleCommand", new { request.Resource.Id });
    }
}
