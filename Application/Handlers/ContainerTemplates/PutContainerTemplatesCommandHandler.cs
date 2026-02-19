using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class PutContainerTemplatesCommandHandler : IRequestHandler<PutContainerTemplatesCommand, List<ContainerTemplateResource>>
{
    private readonly IContainerTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<PutContainerTemplatesCommandHandler> _logger;

    public PutContainerTemplatesCommandHandler(
        IContainerTemplateRepository repository,
        IUnitOfWork unitOfWork,
        ScheduleMapper scheduleMapper,
        ILogger<PutContainerTemplatesCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<List<ContainerTemplateResource>> Handle(PutContainerTemplatesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var existingTemplates = await _repository.GetTemplatesForContainerWithTracking(request.ContainerId);
        var resultTemplates = new List<ContainerTemplate>();

        foreach (var resource in request.Resources)
        {
            _logger.LogInformation("Received resource - Weekday: {Weekday}, TransportMode: {TransportMode}",
                resource.Weekday, resource.TransportMode);

            if (resource.ContainerId == Guid.Empty)
            {
                _logger.LogError("ContainerTemplate has no ContainerId set");
                throw new InvalidOperationException("ContainerTemplate has no ContainerId set");
            }

            if (resource.ContainerId != request.ContainerId)
            {
                _logger.LogError("ContainerTemplate has wrong ContainerId. Expected: {Expected}, Got: {Actual}",
                    request.ContainerId, resource.ContainerId);
                throw new InvalidOperationException($"ContainerTemplate has wrong ContainerId. Expected: {request.ContainerId}, Got: {resource.ContainerId}");
            }

            var existingTemplate = existingTemplates.FirstOrDefault(t =>
                t.Weekday == resource.Weekday &&
                t.IsHoliday == resource.IsHoliday &&
                t.IsWeekdayAndHoliday == resource.IsWeekdayAndHoliday);

            if (existingTemplate != null)
            {
                _logger.LogInformation("Updating template: {TemplateId}", existingTemplate.Id);

                existingTemplate.FromTime = resource.FromTime;
                existingTemplate.UntilTime = resource.UntilTime;
                existingTemplate.StartBase = resource.StartBase;
                existingTemplate.EndBase = resource.EndBase;
                existingTemplate.RouteInfo = resource.RouteInfo != null
                    ? _scheduleMapper.ToRouteInfoEntity(resource.RouteInfo)
                    : null;
                existingTemplate.TransportMode = resource.TransportMode;

                var updateResult = await _repository.PutWithItems(
                    existingTemplate.Id,
                    resource.ContainerTemplateItems);

                foreach (var deletedItem in updateResult.DeletedItems)
                {
                    _logger.LogInformation("ContainerTemplateItem deleted: {ItemId}", deletedItem.Id);
                }

                resultTemplates.Add(existingTemplate);
            }
            else
            {
                _logger.LogInformation("Creating new template for Weekday: {Weekday}", resource.Weekday);

                resource.Id = Guid.Empty;
                foreach (var itemResource in resource.ContainerTemplateItems)
                {
                    itemResource.Id = Guid.Empty;
                }

                var newTemplate = _scheduleMapper.ToContainerTemplateEntity(resource);

                foreach (var item in newTemplate.ContainerTemplateItems)
                {
                    item.Shift = null;
                }

                await _repository.Add(newTemplate);
                resultTemplates.Add(newTemplate);
            }
        }

        var templatesToDelete = existingTemplates.Where(et =>
            !request.Resources.Any(r =>
                et.Weekday == r.Weekday &&
                et.IsHoliday == r.IsHoliday &&
                et.IsWeekdayAndHoliday == r.IsWeekdayAndHoliday)).ToList();

        foreach (var templateToDelete in templatesToDelete)
        {
            _logger.LogInformation("Deleting ContainerTemplate: {TemplateId}", templateToDelete.Id);
            await _repository.Delete(templateToDelete.Id);
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Successfully updated ContainerTemplates for Container: {ContainerId}", request.ContainerId);

        var result = resultTemplates.Select(t => _scheduleMapper.ToContainerTemplateResource(t)).ToList();
        return result;
    }
}
