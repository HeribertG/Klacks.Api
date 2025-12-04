using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class GetContainerTemplatesQueryHandler : IRequestHandler<GetContainerTemplatesQuery, List<ContainerTemplateResource>>
{
    private readonly IContainerTemplateRepository _repository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetContainerTemplatesQueryHandler> _logger;

    public GetContainerTemplatesQueryHandler(
        IContainerTemplateRepository repository,
        ScheduleMapper scheduleMapper,
        ILogger<GetContainerTemplatesQueryHandler> logger)
    {
        _repository = repository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(GetContainerTemplatesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var templates = await _repository.GetTemplatesForContainer(request.ContainerId);
        var resources = templates.Select(t => _scheduleMapper.ToContainerTemplateResource(t)).ToList();

        _logger.LogInformation("Found {Count} ContainerTemplates for Container: {ContainerId}", resources.Count, request.ContainerId);

        return resources;
    }
}
