// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a container shift override. Validates lock and checks no work exists.
/// </summary>
/// <param name="overrideId">The override to update</param>
/// <param name="resource">Updated override data with items</param>
using Klacks.Api.Application.Commands.ContainerShiftOverrides;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class PutContainerShiftOverrideCommandHandler : IRequestHandler<PutContainerShiftOverrideCommand, ContainerShiftOverrideResource>
{
    private const string LockResourceType = "ContainerShiftOverride";

    private readonly IContainerShiftOverrideRepository _repository;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ScheduleMapper _mapper;
    private readonly IUserService _userService;
    private readonly ILogger<PutContainerShiftOverrideCommandHandler> _logger;

    public PutContainerShiftOverrideCommandHandler(
        IContainerShiftOverrideRepository repository,
        IContainerLockRepository lockRepository,
        IUnitOfWork unitOfWork,
        ScheduleMapper mapper,
        IUserService userService,
        ILogger<PutContainerShiftOverrideCommandHandler> logger)
    {
        _repository = repository;
        _lockRepository = lockRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userService = userService;
        _logger = logger;
    }

    public async Task<ContainerShiftOverrideResource> Handle(PutContainerShiftOverrideCommand request, CancellationToken cancellationToken)
    {
        var userId = _userService.GetId() ?? Guid.Empty;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var holdsLock = await _lockRepository.IsHeldBy(LockResourceType, request.ContainerId, userId, instanceId, cancellationToken);
        if (!holdsLock)
        {
            throw new Exception("Cannot save: container shift override is not locked by this session.");
        }

        var existing = await _repository.GetWithTracking(request.OverrideId, cancellationToken);
        if (existing is null)
        {
            throw new Exception($"ContainerShiftOverride {request.OverrideId} not found.");
        }

        var hasWork = await _repository.HasWorkForOverride(existing.ContainerId, existing.Date, cancellationToken);
        if (hasWork)
        {
            throw new Exception("Cannot update override: work entries already exist for this date.");
        }

        existing.FromTime = request.Resource.FromTime;
        existing.UntilTime = request.Resource.UntilTime;
        existing.StartBase = request.Resource.StartBase;
        existing.EndBase = request.Resource.EndBase;
        existing.RouteInfo = request.Resource.RouteInfo != null
            ? _mapper.ToRouteInfoEntity(request.Resource.RouteInfo)
            : null;
        existing.TransportMode = request.Resource.TransportMode;

        var existingItemIds = existing.ContainerShiftOverrideItems
            .Where(i => !i.IsDeleted)
            .Select(i => i.Id)
            .ToHashSet();

        var incomingItemIds = request.Resource.ContainerShiftOverrideItems
            .Where(i => i.Id != Guid.Empty)
            .Select(i => i.Id)
            .ToHashSet();

        foreach (var itemToRemove in existing.ContainerShiftOverrideItems.Where(i => !i.IsDeleted && !incomingItemIds.Contains(i.Id)))
        {
            itemToRemove.IsDeleted = true;
            itemToRemove.DeletedTime = DateTime.UtcNow;
        }

        foreach (var incomingItem in request.Resource.ContainerShiftOverrideItems)
        {
            if (incomingItem.Id != Guid.Empty && existingItemIds.Contains(incomingItem.Id))
            {
                var existingItem = existing.ContainerShiftOverrideItems.First(i => i.Id == incomingItem.Id);
                existingItem.ShiftId = incomingItem.ShiftId;
                existingItem.AbsenceId = incomingItem.AbsenceId;
                existingItem.StartItem = incomingItem.StartItem;
                existingItem.EndItem = incomingItem.EndItem;
                existingItem.BriefingTime = incomingItem.BriefingTime;
                existingItem.DebriefingTime = incomingItem.DebriefingTime;
                existingItem.TravelTimeBefore = incomingItem.TravelTimeBefore;
                existingItem.TravelTimeAfter = incomingItem.TravelTimeAfter;
                existingItem.TimeRangeStartItem = incomingItem.TimeRangeStartItem;
                existingItem.TimeRangeEndItem = incomingItem.TimeRangeEndItem;
                existingItem.TransportMode = incomingItem.TransportMode;
            }
            else
            {
                var newItem = _mapper.ToContainerShiftOverrideItemEntity(incomingItem);
                newItem.ContainerShiftOverrideId = existing.Id;
                newItem.Shift = null;
                newItem.Absence = null;
                existing.ContainerShiftOverrideItems.Add(newItem);
            }
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Updated ContainerShiftOverride {OverrideId}", request.OverrideId);

        return _mapper.ToContainerShiftOverrideResource(existing);
    }
}
