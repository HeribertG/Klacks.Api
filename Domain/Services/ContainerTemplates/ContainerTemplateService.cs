// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ContainerTemplates;

public class ContainerTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ContainerTemplateService> _logger;

    public ContainerTemplateService(
        IUnitOfWork unitOfWork,
        ILogger<ContainerTemplateService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContainerTemplateUpdateResult> UpdateContainerTemplate(
        IContainerTemplateRepository repository,
        Guid templateId,
        List<ContainerTemplateItem> itemsToUpdate)
    {
        var result = new ContainerTemplateUpdateResult();

        _logger.LogInformation("Updating ContainerTemplate: {TemplateId}", templateId);

        var existingList = await repository.GetItemsForTemplate(templateId, tracked: false);
        _logger.LogInformation("Found {Count} existing items for template", existingList.Count);

        foreach (var item in itemsToUpdate)
        {
            if (item.Id != Guid.Empty)
            {
                _logger.LogInformation("Updating ContainerTemplateItem: {ItemId}", item.Id);
                await repository.UpdateItem(item);
                result.UpdatedItems.Add(item.Id);

                var existingItem = existingList.FirstOrDefault(e => e.Id == item.Id);
                if (existingItem != null)
                {
                    existingList.Remove(existingItem);
                }
            }
            else
            {
                _logger.LogInformation("Creating new ContainerTemplateItem for Template: {TemplateId}", templateId);
                var createdId = await repository.CreateItem(templateId, item);
                result.CreatedItems.Add(createdId);
            }
        }

        foreach (var itemToDelete in existingList)
        {
            _logger.LogInformation("Deleting ContainerTemplateItem: {ItemId}", itemToDelete.Id);
            await repository.DeleteItem(itemToDelete.Id);
            result.DeletedItems.Add(itemToDelete);
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation(
            "Update completed. Created: {Created}, Updated: {Updated}, Deleted: {Deleted}",
            result.CreatedItems.Count,
            result.UpdatedItems.Count,
            result.DeletedItems.Count);

        return result;
    }
}
