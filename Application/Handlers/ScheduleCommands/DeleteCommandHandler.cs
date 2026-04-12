// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for soft-deleting a ScheduleCommand.
/// </summary>
/// <param name="request">Contains the ID of the command to delete</param>
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleCommands;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ScheduleCommandResource>, ScheduleCommandResource?>
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleCommandResource?> Handle(DeleteCommand<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        var existing = await _scheduleCommandRepository.Get(request.Id);
        if (existing == null)
        {
            return null;
        }

        var resource = _scheduleMapper.ToScheduleCommandResource(existing);

        await _scheduleCommandRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        return resource;
    }
}
