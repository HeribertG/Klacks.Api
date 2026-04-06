// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for refreshing the heartbeat timestamp on an existing container lock.
/// Returns Acquired=false when the lock no longer exists or belongs to another user.
/// </summary>
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public class HeartbeatContainerLockCommandHandler : BaseHandler, IRequestHandler<HeartbeatContainerLockCommand, ContainerLockResource>
{
    private readonly IContainerLockRepository _repository;
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;

    public HeartbeatContainerLockCommandHandler(
        IContainerLockRepository repository,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ILogger<HeartbeatContainerLockCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _userService = userService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ContainerLockResource> Handle(HeartbeatContainerLockCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetById(request.LockId, cancellationToken);
        var currentUserId = _userService.GetId() ?? Guid.Empty;

        if (existing == null || existing.UserId != currentUserId)
        {
            return new ContainerLockResource { Acquired = false };
        }

        existing.LastHeartbeatAt = DateTime.UtcNow;
        await _repository.Update(existing, cancellationToken);
        await _unitOfWork.CompleteAsync();

        return new ContainerLockResource
        {
            Id = existing.Id,
            ResourceType = existing.ResourceType,
            ResourceId = existing.ResourceId,
            UserId = existing.UserId,
            UserName = existing.UserName,
            InstanceId = existing.InstanceId,
            AcquiredAt = existing.AcquiredAt,
            LastHeartbeatAt = existing.LastHeartbeatAt,
            Acquired = true,
        };
    }
}
