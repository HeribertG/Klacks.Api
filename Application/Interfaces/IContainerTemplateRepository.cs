using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.ContainerTemplates;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IContainerTemplateRepository : IBaseRepository<ContainerTemplate>
{
    new Task<ContainerTemplate?> Get(Guid id);

    Task<List<ContainerTemplate>> GetTemplatesForContainer(Guid containerId);

    Task<List<ContainerTemplate>> GetTemplatesForContainerWithTracking(Guid containerId);

    IQueryable<ContainerTemplate> GetQuery();

    Task<List<Guid>> GetUsedShiftIds(Guid? excludeContainerId = null, CancellationToken cancellationToken = default);

    Task<List<ContainerTemplateItem>> GetItemsForTemplate(Guid templateId, bool tracked);

    Task UpdateItem(ContainerTemplateItemResource itemResource);

    Task<Guid> CreateItem(Guid templateId, ContainerTemplateItemResource itemResource);

    Task DeleteItem(Guid itemId);

    Task<ContainerTemplateUpdateResult> PutWithItems(Guid templateId, List<ContainerTemplateItemResource> items);
}
