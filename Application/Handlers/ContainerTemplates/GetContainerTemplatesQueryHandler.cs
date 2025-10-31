using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class GetContainerTemplatesQueryHandler : IRequestHandler<GetContainerTemplatesQuery, List<ContainerTemplateResource>>
{
    private readonly IContainerTemplateRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetContainerTemplatesQueryHandler> _logger;

    public GetContainerTemplatesQueryHandler(
        IContainerTemplateRepository repository,
        IMapper mapper,
        ILogger<GetContainerTemplatesQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(GetContainerTemplatesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var templates = await _repository.GetTemplatesForContainer(request.ContainerId);
        var resources = _mapper.Map<List<ContainerTemplateResource>>(templates);

        _logger.LogInformation("Found {Count} ContainerTemplates for Container: {ContainerId}", resources.Count, request.ContainerId);

        return resources;
    }
}
