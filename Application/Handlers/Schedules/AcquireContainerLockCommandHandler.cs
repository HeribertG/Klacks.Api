// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for acquiring an exclusive edit lock on a container resource.
/// If the resource is already locked by another user with a fresh heartbeat, the existing
/// lock is returned with Acquired=false. Stale locks are removed first. Idempotent for the
/// same user (refreshes heartbeat). Only the resource types of ContainerLockResourceTypes are accepted (400 otherwise).
/// A lock keyed by a container needs an existing container the caller may see; a lock keyed by a work needs an existing
/// work whose client and scenario the caller may see and whose shift the caller may touch. Missing and hidden resources
/// are both answered with 404, and all of this is checked before stale locks are swept or the holder is revealed.
/// </summary>
/// <param name="containerVisibilityGuard">Refuses missing and hidden containers alike</param>
/// <param name="workReadRepository">Reads the stored work a ContainerWork lock is keyed by</param>
/// <param name="rowAccessGuard">Decides whether the caller may see the work's client and its stored scenario</param>
/// <param name="shiftVisibilityGuard">Decides whether the caller may touch the work's shift</param>
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
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
    private readonly IContainerVisibilityGuard _containerVisibilityGuard;
    private readonly IContainerWorkChildrenReadRepository _workReadRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public AcquireContainerLockCommandHandler(
        IContainerLockRepository repository,
        IUserService userService,
        IUnitOfWork unitOfWork,
        IContainerVisibilityGuard containerVisibilityGuard,
        IContainerWorkChildrenReadRepository workReadRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<AcquireContainerLockCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _userService = userService;
        _unitOfWork = unitOfWork;
        _containerVisibilityGuard = containerVisibilityGuard;
        _workReadRepository = workReadRepository;
        _rowAccessGuard = rowAccessGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<ContainerLockResource> Handle(AcquireContainerLockCommand request, CancellationToken cancellationToken)
    {
        await EnsureResourceVisibleAsync(request.ResourceType, request.ResourceId, cancellationToken);

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

    private async Task EnsureResourceVisibleAsync(string? resourceType, Guid resourceId, CancellationToken cancellationToken)
    {
        switch (ContainerLockResourceTypes.FindKey(resourceType))
        {
            case ContainerLockKey.Container:
                await _containerVisibilityGuard.EnsureContainerVisibleAsync(resourceId, cancellationToken);
                return;
            case ContainerLockKey.Work:
                await EnsureWorkVisibleAsync(resourceId, cancellationToken);
                return;
            default:
                throw new BadRequestException(string.Format(ContainerLockMessages.UnknownResourceType, resourceType));
        }
    }

    /// <summary>
    /// Same rule as the work paths: the scenario token comes from the stored work, never from the request.
    /// </summary>
    /// <param name="workId">Work the ContainerWork lock is keyed by</param>
    private async Task EnsureWorkVisibleAsync(Guid workId, CancellationToken cancellationToken)
    {
        var work = await _workReadRepository.GetParentWorkNoTracking(workId, cancellationToken);
        var visible = work != null
            && await _rowAccessGuard.IsVisibleAsync(work.ClientId, work.AnalyseToken, cancellationToken)
            && await _shiftVisibilityGuard.IsVisibleAsync(work.ShiftId, cancellationToken);
        if (!visible)
        {
            _logger.LogWarning("Container work lock refused on hidden or missing work {WorkId}", workId);
            throw new KeyNotFoundException(string.Format(ContainerLockMessages.WorkNotFound, workId));
        }
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
