// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads all templates of a container. A missing or hidden container is answered with 404 (KeyNotFoundException);
/// an existing visible container without templates returns an empty list.
/// </summary>
/// <param name="containerVisibilityGuard">Refuses missing and hidden containers alike</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class GetContainerTemplatesQueryHandler : IRequestHandler<GetContainerTemplatesQuery, List<ContainerTemplateResource>>
{
    private readonly IContainerTemplateRepository _repository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IContainerVisibilityGuard _containerVisibilityGuard;
    private readonly ILogger<GetContainerTemplatesQueryHandler> _logger;

    public GetContainerTemplatesQueryHandler(
        IContainerTemplateRepository repository,
        ScheduleMapper scheduleMapper,
        IContainerVisibilityGuard containerVisibilityGuard,
        ILogger<GetContainerTemplatesQueryHandler> logger)
    {
        _repository = repository;
        _scheduleMapper = scheduleMapper;
        _containerVisibilityGuard = containerVisibilityGuard;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(GetContainerTemplatesQuery request, CancellationToken cancellationToken)
    {
        await _containerVisibilityGuard.EnsureContainerVisibleAsync(request.ContainerId, cancellationToken);

        _logger.LogInformation("Getting all ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var templates = await _repository.GetTemplatesForContainer(request.ContainerId);
        var resources = templates.Select(t => _scheduleMapper.ToContainerTemplateResource(t)).ToList();

        _logger.LogInformation("Found {Count} ContainerTemplates for Container: {ContainerId}", resources.Count, request.ContainerId);

        return resources;
    }
}
