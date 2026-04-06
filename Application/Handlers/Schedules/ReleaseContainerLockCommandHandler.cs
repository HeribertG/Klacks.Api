// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for releasing a container lock. Only the owning user can release it.
/// </summary>
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public class ReleaseContainerLockCommandHandler : BaseHandler, IRequestHandler<ReleaseContainerLockCommand, bool>
{
    private readonly IContainerLockRepository _repository;
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;

    public ReleaseContainerLockCommandHandler(
        IContainerLockRepository repository,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ILogger<ReleaseContainerLockCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _userService = userService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ReleaseContainerLockCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetById(request.LockId, cancellationToken);
        var currentUserId = _userService.GetId() ?? Guid.Empty;

        if (existing == null || existing.UserId != currentUserId)
        {
            return false;
        }

        await _repository.Delete(existing, cancellationToken);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
