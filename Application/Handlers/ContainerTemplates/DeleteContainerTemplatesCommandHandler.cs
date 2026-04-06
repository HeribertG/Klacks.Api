// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class DeleteContainerTemplatesCommandHandler : IRequestHandler<DeleteContainerTemplatesCommand, List<ContainerTemplateResource>>
{
    private const string LockResourceType = "ContainerTemplate";

    private readonly IContainerTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUserService _userService;
    private readonly ILogger<DeleteContainerTemplatesCommandHandler> _logger;

    public DeleteContainerTemplatesCommandHandler(
        IContainerTemplateRepository repository,
        IUnitOfWork unitOfWork,
        ScheduleMapper scheduleMapper,
        IContainerLockRepository lockRepository,
        IUserService userService,
        ILogger<DeleteContainerTemplatesCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _scheduleMapper = scheduleMapper;
        _lockRepository = lockRepository;
        _userService = userService;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(DeleteContainerTemplatesCommand request, CancellationToken cancellationToken)
    {
        var userId = _userService.GetId() ?? Guid.Empty;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var holdsLock = await _lockRepository.IsHeldBy(LockResourceType, request.ContainerId, userId, instanceId, cancellationToken);
        if (!holdsLock)
        {
            throw new ContainerLockedException("Cannot delete: container template is not locked by this session.");
        }

        _logger.LogInformation("Deleting all ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var templates = await _repository.GetTemplatesForContainer(request.ContainerId);

        var deletedTemplates = templates.Select(t => _scheduleMapper.ToContainerTemplateResource(t)).ToList();

        foreach (var template in templates)
        {
            _repository.Remove(template);
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Successfully deleted {Count} ContainerTemplates for Container: {ContainerId}",
            deletedTemplates.Count, request.ContainerId);

        return deletedTemplates;
    }
}
