using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Interfaces;
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
    private readonly ILogger<PostContainerTemplatesCommandHandler> _logger;

    public PostContainerTemplatesCommandHandler(
        IContainerTemplateRepository repository,
        IUnitOfWork unitOfWork,
        ScheduleMapper scheduleMapper,
        ILogger<PostContainerTemplatesCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _scheduleMapper = scheduleMapper;
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
            var template = _scheduleMapper.ToContainerTemplateEntity(resource);

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

        var result = createdTemplates.Select(t => _scheduleMapper.ToContainerTemplateResource(t)).ToList();
        return result;
    }
}
