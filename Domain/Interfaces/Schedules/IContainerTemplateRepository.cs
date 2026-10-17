// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerTemplateRepository : IBaseRepository<ContainerTemplate>
{
    new Task<ContainerTemplate?> Get(Guid id);

    Task<List<ContainerTemplate>> GetTemplatesForContainer(Guid containerId);

    Task<List<ContainerTemplate>> GetTemplatesForContainerWithTracking(Guid containerId);

    IQueryable<ContainerTemplate> GetQuery();

    Task<List<Guid>> GetUsedShiftIds(Guid? excludeContainerId = null, CancellationToken cancellationToken = default);

    Task<List<ContainerTemplateItem>> GetItemsForTemplate(Guid templateId, bool tracked);

    /// <summary>
    /// Updates a stored item of the given template. An item that is missing or belongs to another template throws
    /// KeyNotFoundException with the same text, so a foreign item id cannot be overwritten.
    /// </summary>
    /// <param name="templateId">Template the item must belong to</param>
    /// <param name="item">Item values to apply, keyed by item.Id</param>
    Task UpdateItem(Guid templateId, ContainerTemplateItem item);

    Task<Guid> CreateItem(Guid templateId, ContainerTemplateItem item);

    Task DeleteItem(Guid itemId);

    Task<ContainerTemplateUpdateResult> PutWithItems(Guid templateId, List<ContainerTemplateItem> items);
}
