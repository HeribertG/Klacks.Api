using AutoMapper;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class DeleteContainerTemplatesCommandHandler : IRequestHandler<DeleteContainerTemplatesCommand, List<ContainerTemplateResource>>
{
    private readonly IContainerTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DeleteContainerTemplatesCommandHandler> _logger;

    public DeleteContainerTemplatesCommandHandler(
        IContainerTemplateRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<DeleteContainerTemplatesCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(DeleteContainerTemplatesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting all ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var templates = await _repository.GetTemplatesForContainer(request.ContainerId);

        var deletedTemplates = _mapper.Map<List<ContainerTemplateResource>>(templates);

        foreach (var template in templates)
        {
            await _repository.Delete(template.Id);
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Successfully deleted {Count} ContainerTemplates for Container: {ContainerId}",
            deletedTemplates.Count, request.ContainerId);

        return deletedTemplates;
    }
}
