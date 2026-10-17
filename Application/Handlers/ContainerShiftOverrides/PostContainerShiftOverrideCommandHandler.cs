// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for creating a container shift override. A missing or hidden container is answered with 404 before the lock
/// is checked; an item with a task shift the caller may not see that the container does not reference yet (in its
/// templates or overrides) is answered with 404 before anything is written.
/// </summary>
/// <param name="containerId">The container shift to override</param>
/// <param name="resource">The override data with items</param>
/// <param name="containerVisibilityGuard">Refuses missing and hidden containers and new hidden task shifts</param>
using Klacks.Api.Application.Commands.ContainerShiftOverrides;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class PostContainerShiftOverrideCommandHandler : IRequestHandler<PostContainerShiftOverrideCommand, ContainerShiftOverrideResource>
{
    private readonly IContainerShiftOverrideRepository _repository;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ScheduleMapper _mapper;
    private readonly IUserService _userService;
    private readonly IContainerVisibilityGuard _containerVisibilityGuard;
    private readonly ILogger<PostContainerShiftOverrideCommandHandler> _logger;

    public PostContainerShiftOverrideCommandHandler(
        IContainerShiftOverrideRepository repository,
        IContainerLockRepository lockRepository,
        IUnitOfWork unitOfWork,
        ScheduleMapper mapper,
        IUserService userService,
        IContainerVisibilityGuard containerVisibilityGuard,
        ILogger<PostContainerShiftOverrideCommandHandler> logger)
    {
        _repository = repository;
        _lockRepository = lockRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userService = userService;
        _containerVisibilityGuard = containerVisibilityGuard;
        _logger = logger;
    }

    public async Task<ContainerShiftOverrideResource> Handle(PostContainerShiftOverrideCommand request, CancellationToken cancellationToken)
    {
        await _containerVisibilityGuard.EnsureContainerVisibleAsync(request.ContainerId, cancellationToken);

        var userId = _userService.GetId() ?? Guid.Empty;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var holdsLock = await _lockRepository.IsHeldBy(ContainerLockResourceTypes.ContainerShiftOverride, request.ContainerId, userId, instanceId, cancellationToken);
        if (!holdsLock)
        {
            throw new ContainerLockedException("Container shift override is not locked by this session.");
        }

        await _containerVisibilityGuard.EnsureItemShiftsVisibleAsync(
            request.ContainerId,
            request.Resource.ContainerShiftOverrideItems.Select(i => i.ShiftId),
            cancellationToken);

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
