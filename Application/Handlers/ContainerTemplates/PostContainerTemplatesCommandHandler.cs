// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates templates of a container under the container template lock. A missing or hidden container, or an item
/// with a task shift the caller may not see and the container does not reference yet, is answered with 404 before
/// anything is written.
/// </summary>
/// <param name="containerVisibilityGuard">Refuses missing and hidden containers and new hidden task shifts</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class PostContainerTemplatesCommandHandler : IRequestHandler<PostContainerTemplatesCommand, List<ContainerTemplateResource>>
{
    private readonly IContainerTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUserService _userService;
    private readonly IContainerVisibilityGuard _containerVisibilityGuard;
    private readonly ILogger<PostContainerTemplatesCommandHandler> _logger;

    public PostContainerTemplatesCommandHandler(
        IContainerTemplateRepository repository,
        IUnitOfWork unitOfWork,
        ScheduleMapper scheduleMapper,
        IContainerLockRepository lockRepository,
        IUserService userService,
        IContainerVisibilityGuard containerVisibilityGuard,
        ILogger<PostContainerTemplatesCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _scheduleMapper = scheduleMapper;
        _lockRepository = lockRepository;
        _userService = userService;
        _containerVisibilityGuard = containerVisibilityGuard;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(PostContainerTemplatesCommand request, CancellationToken cancellationToken)
    {
        await _containerVisibilityGuard.EnsureContainerVisibleAsync(request.ContainerId, cancellationToken);

        var userId = _userService.GetId() ?? Guid.Empty;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var holdsLock = await _lockRepository.IsHeldBy(ContainerLockResourceTypes.ContainerTemplate, request.ContainerId, userId, instanceId, cancellationToken);
        if (!holdsLock)
        {
            throw new ContainerLockedException("Cannot save: container template is not locked by this session.");
        }

        await _containerVisibilityGuard.EnsureItemShiftsVisibleAsync(
            request.ContainerId,
            request.Resources.SelectMany(r => r.ContainerTemplateItems).Select(i => i.ShiftId),
            cancellationToken);

        _logger.LogInformation("Creating {Count} ContainerTemplates for Container: {ContainerId}",
            request.Resources.Count, request.ContainerId);

        var createdTemplates = new List<ContainerTemplate>();

        foreach (var resource in request.Resources)
        {
            resource.ContainerId = request.ContainerId;
            var template = _scheduleMapper.ToContainerTemplateEntity(resource);

            foreach (var item in template.ContainerTemplateItems)
            {
                item.Shift = null;
                item.Absence = null;
            }

            await _repository.Add(template);
            createdTemplates.Add(template);
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Successfully created {Count} ContainerTemplates for Container: {ContainerId}",
            createdTemplates.Count, request.ContainerId);

        var result = createdTemplates.Select(t => _scheduleMapper.ToContainerTemplateResource(t)).ToList();
        return result;
    }
}
