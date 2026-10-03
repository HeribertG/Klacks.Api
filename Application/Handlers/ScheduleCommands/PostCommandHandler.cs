// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for creating a new ScheduleCommand. A command for a client outside the caller's group visibility
/// is refused exactly like a command for a client that does not exist; nothing is written.
/// </summary>
/// <param name="request">Contains the ScheduleCommandResource to create</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleCommands;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ScheduleCommandResource>, ScheduleCommandResource?>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleCommandResource?> Handle(PostCommand<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scheduleCommand = _scheduleMapper.ToScheduleCommandEntity(request.Resource);

            if (!await _clientVisibilityGuard.IsVisibleAsync(scheduleCommand.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {scheduleCommand.ClientId} not found");
            }

            await _scheduleCommandRepository.Add(scheduleCommand);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToScheduleCommandResource(scheduleCommand);
        }, "CreateScheduleCommand", new { request.Resource.Id });
    }
}
