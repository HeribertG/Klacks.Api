// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for acquiring an exclusive edit lock on a container resource.
/// If the resource is already locked by another user with a fresh heartbeat, the existing
/// lock is returned with Acquired=false. Stale locks are removed first. Idempotent for the
/// same user (refreshes heartbeat).
/// </summary>
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public class AcquireContainerLockCommandHandler : BaseHandler, IRequestHandler<AcquireContainerLockCommand, ContainerLockResource>
{
    private const int StaleSeconds = 90;

    private readonly IContainerLockRepository _repository;
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;

    public AcquireContainerLockCommandHandler(
        IContainerLockRepository repository,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ILogger<AcquireContainerLockCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _userService = userService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ContainerLockResource> Handle(AcquireContainerLockCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var staleThreshold = now.AddSeconds(-StaleSeconds);
        var currentUserId = _userService.GetId() ?? Guid.Empty;
        var currentUserName = _userService.GetUserName();

        await _repository.DeleteStale(request.ResourceType, request.ResourceId, staleThreshold, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var existing = await _repository.GetByResource(request.ResourceType, request.ResourceId, cancellationToken);

        if (existing != null && (existing.UserId != currentUserId || existing.InstanceId != request.InstanceId))
        {
            var isSelf = existing.UserId == currentUserId;
            return ToResource(existing, acquired: false, isSelfConflict: isSelf);
        }

        if (existing != null)
        {
            existing.LastHeartbeatAt = now;
            await _repository.Update(existing, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return ToResource(existing, acquired: true, isSelfConflict: false);
        }

        var newLock = new ContainerLock
        {
            Id = Guid.NewGuid(),
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            UserId = currentUserId,
            UserName = currentUserName,
            InstanceId = request.InstanceId,
            AcquiredAt = now,
            LastHeartbeatAt = now,
        };

        await _repository.Add(newLock, cancellationToken);
        await _unitOfWork.CompleteAsync();

        return ToResource(newLock, acquired: true, isSelfConflict: false);
    }

    private static ContainerLockResource ToResource(ContainerLock l, bool acquired, bool isSelfConflict) => new()
    {
        Id = l.Id,
        ResourceType = l.ResourceType,
        ResourceId = l.ResourceId,
        UserId = l.UserId,
        UserName = l.UserName,
        InstanceId = l.InstanceId,
        AcquiredAt = l.AcquiredAt,
        LastHeartbeatAt = l.LastHeartbeatAt,
        Acquired = acquired,
        IsSelfConflict = isSelfConflict,
    };
}
