// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for updating an existing ScheduleCommand. A command whose stored owner or whose new owner is
/// outside the caller's group visibility is answered exactly like a command that does not exist; nothing is
/// written.
/// </summary>
/// <param name="request">Contains the updated ScheduleCommandResource</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>

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
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
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

            if (!await _clientVisibilityGuard.AreAllVisibleAsync(
                    [existing.ClientId, scheduleCommand.ClientId], cancellationToken))
            {
                return null;
            }

            var updated = await _scheduleCommandRepository.Put(scheduleCommand);
            await _unitOfWork.CompleteAsync();

            return updated != null ? _scheduleMapper.ToScheduleCommandResource(updated) : null;
        }, "UpdateScheduleCommand", new { request.Resource.Id });
    }
}
