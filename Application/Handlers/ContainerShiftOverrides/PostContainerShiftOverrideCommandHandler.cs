// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a container shift override. Validates lock ownership.
/// </summary>
/// <param name="containerId">The container shift to override</param>
/// <param name="resource">The override data with items</param>
using Klacks.Api.Application.Commands.ContainerShiftOverrides;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class PostContainerShiftOverrideCommandHandler : IRequestHandler<PostContainerShiftOverrideCommand, ContainerShiftOverrideResource>
{
    private const string LockResourceType = "ContainerShiftOverride";

    private readonly IContainerShiftOverrideRepository _repository;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ScheduleMapper _mapper;
    private readonly IUserService _userService;
    private readonly ILogger<PostContainerShiftOverrideCommandHandler> _logger;

    public PostContainerShiftOverrideCommandHandler(
        IContainerShiftOverrideRepository repository,
        IContainerLockRepository lockRepository,
        IUnitOfWork unitOfWork,
        ScheduleMapper mapper,
        IUserService userService,
        ILogger<PostContainerShiftOverrideCommandHandler> logger)
    {
        _repository = repository;
        _lockRepository = lockRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userService = userService;
        _logger = logger;
    }

    public async Task<ContainerShiftOverrideResource> Handle(PostContainerShiftOverrideCommand request, CancellationToken cancellationToken)
    {
        var userId = _userService.GetId() ?? Guid.Empty;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var holdsLock = await _lockRepository.IsHeldBy(LockResourceType, request.ContainerId, userId, instanceId, cancellationToken);
        if (!holdsLock)
        {
            throw new ContainerLockedException("Container shift override is not locked by this session.");
        }

        request.Resource.ContainerId = request.ContainerId;
        var entity = _mapper.ToContainerShiftOverrideEntity(request.Resource);

        foreach (var item in entity.ContainerShiftOverrideItems)
        {
            item.Shift = null;
            item.Absence = null;
        }

        await _repository.Add(entity);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Created ContainerShiftOverride for container={ContainerId} date={Date}",
            request.ContainerId, request.Resource.Date);

        return _mapper.ToContainerShiftOverrideResource(entity);
    }
}
