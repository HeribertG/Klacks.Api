// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a new ScheduleCommand.
/// </summary>
/// <param name="request">Contains the ScheduleCommandResource to create</param>
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
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IScheduleCommandRepository scheduleCommandRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleCommandResource?> Handle(PostCommand<ScheduleCommandResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scheduleCommand = _scheduleMapper.ToScheduleCommandEntity(request.Resource);
            await _scheduleCommandRepository.Add(scheduleCommand);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToScheduleCommandResource(scheduleCommand);
        }, "CreateScheduleCommand", new { request.Resource.Id });
    }
}
