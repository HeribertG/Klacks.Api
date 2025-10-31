using AutoMapper;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class PutContainerTemplatesCommandHandler : IRequestHandler<PutContainerTemplatesCommand, List<ContainerTemplateResource>>
{
    private readonly IContainerTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PutContainerTemplatesCommandHandler> _logger;

    public PutContainerTemplatesCommandHandler(
        IContainerTemplateRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PutContainerTemplatesCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(PutContainerTemplatesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var existingTemplates = await _repository.GetTemplatesForContainer(request.ContainerId);

        _logger.LogInformation("Deleting {Count} existing ContainerTemplates", existingTemplates.Count);
        foreach (var template in existingTemplates)
        {
            await _repository.Delete(template.Id);
        }

        var updatedTemplates = new List<ContainerTemplate>();

        _logger.LogInformation("Creating {Count} new ContainerTemplates", request.Resources.Count);
        foreach (var resource in request.Resources)
        {
            resource.ContainerId = request.ContainerId;
            var template = _mapper.Map<ContainerTemplate>(resource);

            await _repository.Add(template);
            updatedTemplates.Add(template);
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Successfully updated ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var result = _mapper.Map<List<ContainerTemplateResource>>(updatedTemplates);
        return result;
    }
}
