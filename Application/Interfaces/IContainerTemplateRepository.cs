using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IContainerTemplateRepository : IBaseRepository<ContainerTemplate>
{
    new Task<ContainerTemplate?> Get(Guid id);

    Task<List<ContainerTemplate>> GetTemplatesForContainer(Guid containerId);

    IQueryable<ContainerTemplate> GetQuery();
}
