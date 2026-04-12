// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating an existing ScheduleCommand.
/// </summary>
/// <param name="request">Contains the updated ScheduleCommandResource</param>
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
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
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
            var updated = await _scheduleCommandRepository.Put(scheduleCommand);
            await _unitOfWork.CompleteAsync();

            return updated != null ? _scheduleMapper.ToScheduleCommandResource(updated) : null;
        }, "UpdateScheduleCommand", new { request.Resource.Id });
    }
}
