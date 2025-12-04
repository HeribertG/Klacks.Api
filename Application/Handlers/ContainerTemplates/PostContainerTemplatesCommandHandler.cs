using AutoMapper;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class PostContainerTemplatesCommandHandler : IRequestHandler<PostContainerTemplatesCommand, List<ContainerTemplateResource>>
{
    private readonly IContainerTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PostContainerTemplatesCommandHandler> _logger;

    public PostContainerTemplatesCommandHandler(
        IContainerTemplateRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PostContainerTemplatesCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(PostContainerTemplatesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating {Count} ContainerTemplates for Container: {ContainerId}",
            request.Resources.Count, request.ContainerId);

        var createdTemplates = new List<ContainerTemplate>();

        foreach (var resource in request.Resources)
        {
            resource.ContainerId = request.ContainerId;
            var template = _mapper.Map<ContainerTemplate>(resource);

            foreach (var item in template.ContainerTemplateItems)
            {
                item.Shift = null;
            }

            await _repository.Add(template);
            createdTemplates.Add(template);
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Successfully created {Count} ContainerTemplates for Container: {ContainerId}",
            createdTemplates.Count, request.ContainerId);

        var result = _mapper.Map<List<ContainerTemplateResource>>(createdTemplates);
        return result;
    }
}
