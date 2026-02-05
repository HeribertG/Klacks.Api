using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Domain.Services.ContainerTemplates;

public class ContainerTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ContainerTemplateService> _logger;

    public ContainerTemplateService(
        IUnitOfWork unitOfWork,
        ILogger<ContainerTemplateService> _logger)
    {
        _unitOfWork = unitOfWork;
        this._logger = _logger;
    }

    public async Task<ContainerTemplateUpdateResult> UpdateContainerTemplate(
        IContainerTemplateRepository repository,
        Guid templateId,
        List<ContainerTemplateItemResource> itemsToUpdate)
    {
        var result = new ContainerTemplateUpdateResult();

        _logger.LogInformation("Updating ContainerTemplate: {TemplateId}", templateId);

        var existingList = await repository.GetItemsForTemplate(templateId, tracked: false);
        _logger.LogInformation("Found {Count} existing items for template", existingList.Count);

        foreach (var itemResource in itemsToUpdate)
        {
            if (itemResource.Id != Guid.Empty)
            {
                _logger.LogInformation("Updating ContainerTemplateItem: {ItemId}", itemResource.Id);
                await repository.UpdateItem(itemResource);
                result.UpdatedItems.Add(itemResource.Id);

                var existingItem = existingList.FirstOrDefault(e => e.Id == itemResource.Id);
                if (existingItem != null)
                {
                    existingList.Remove(existingItem);
                }
            }
            else
            {
                _logger.LogInformation("Creating new ContainerTemplateItem for Template: {TemplateId}", templateId);
                var createdId = await repository.CreateItem(templateId, itemResource);
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

public class ContainerTemplateUpdateResult
{
    public List<Guid> CreatedItems { get; set; } = new();
    public List<Guid> UpdatedItems { get; set; } = new();
    public List<ContainerTemplateItem> DeletedItems { get; set; } = new();
}
