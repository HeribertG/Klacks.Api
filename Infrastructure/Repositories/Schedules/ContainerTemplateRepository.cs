using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.ContainerTemplates;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ContainerTemplateRepository : BaseRepository<ContainerTemplate>, IContainerTemplateRepository
{
    private readonly DataBaseContext context;
    private readonly EntityCollectionUpdateService _collectionUpdateService;
    private readonly ContainerTemplateService _templateService;

    public ContainerTemplateRepository(
        DataBaseContext context,
        ILogger<ContainerTemplate> logger,
        EntityCollectionUpdateService collectionUpdateService,
        ContainerTemplateService templateService)
        : base(context, logger)
    {
        this.context = context;
        _collectionUpdateService = collectionUpdateService;
        _templateService = templateService;
    }

    public new async Task Add(ContainerTemplate template)
    {
        foreach (var item in template.ContainerTemplateItems)
        {
            item.ContainerTemplateId = template.Id;
        }

        await context.ContainerTemplate.AddAsync(template);
        Logger.LogInformation("ContainerTemplate added: {TemplateId}, Items count: {Count}",
            template.Id, template.ContainerTemplateItems.Count);
    }

    public new async Task<ContainerTemplate?> Put(ContainerTemplate template)
    {
        var existingTemplate = await context.ContainerTemplate
            .Include(t => t.Shift)
            .Include(t => t.ContainerTemplateItems)
                .ThenInclude(i => i.Shift)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == template.Id);

        if (existingTemplate == null)
        {
            Logger.LogWarning("ContainerTemplate not found for update: {TemplateId}", template.Id);
            return null;
        }

        var entry = context.Entry(existingTemplate);
        entry.CurrentValues.SetValues(template);
        entry.State = EntityState.Modified;

        _collectionUpdateService.UpdateCollection(
            existingTemplate.ContainerTemplateItems,
            template.ContainerTemplateItems,
            existingTemplate.Id,
            (item, templateId) => item.ContainerTemplateId = templateId);

        Logger.LogInformation("ContainerTemplate updated: {TemplateId}, Items count: {Count}",
            template.Id, existingTemplate.ContainerTemplateItems.Count);

        return existingTemplate;
    }

    public async Task<ContainerTemplateUpdateResult> PutWithItems(
        Guid templateId,
        List<ContainerTemplateItemResource> items)
    {
        Logger.LogInformation("Updating ContainerTemplate with items: {TemplateId}", templateId);

        var result = await _templateService.UpdateContainerTemplate(this, templateId, items);

        Logger.LogInformation("ContainerTemplate items updated: {TemplateId}", templateId);

        return result;
    }

    public new async Task<ContainerTemplate?> Get(Guid id)
    {
        Logger.LogInformation("Fetching ContainerTemplate with ID: {TemplateId}", id);

        var template = await context.ContainerTemplate
            .Where(t => t.Id == id)
            .Include(t => t.Shift)
            .Include(t => t.ContainerTemplateItems)
                .ThenInclude(i => i.Shift)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (template != null)
        {
            Logger.LogInformation("ContainerTemplate with ID: {TemplateId} found with {ItemsCount} items.",
                id, template.ContainerTemplateItems.Count);
        }
        else
        {
            Logger.LogWarning("ContainerTemplate with ID: {TemplateId} not found.", id);
        }

        return template;
    }

    public async Task<List<ContainerTemplate>> GetTemplatesForContainer(Guid containerId)
    {
        Logger.LogInformation("Fetching all ContainerTemplates for Container: {ContainerId}", containerId);

        var templates = await context.ContainerTemplate
            .Where(t => t.ContainerId == containerId)
            .Include(t => t.Shift)
            .Include(t => t.ContainerTemplateItems)
                .ThenInclude(i => i.Shift)
                    .ThenInclude(s => s.Client)
                        .ThenInclude(c => c.Addresses)
            .AsSplitQuery()
            .AsNoTracking()
            .OrderBy(t => t.Weekday)
            .ThenBy(t => t.IsHoliday)
            .ToListAsync();

        Logger.LogInformation("Found {Count} templates for Container: {ContainerId}",
            templates.Count, containerId);

        return templates;
    }

    public async Task<List<ContainerTemplate>> GetTemplatesForContainerWithTracking(Guid containerId)
    {
        Logger.LogInformation("Fetching all ContainerTemplates with tracking for Container: {ContainerId}", containerId);

        var templates = await context.ContainerTemplate
            .Where(t => t.ContainerId == containerId)
            .Include(t => t.Shift)
            .Include(t => t.ContainerTemplateItems)
                .ThenInclude(i => i.Shift)
            .AsSplitQuery()
            .OrderBy(t => t.Weekday)
            .ThenBy(t => t.IsHoliday)
            .ToListAsync();

        Logger.LogInformation("Found {Count} templates with tracking for Container: {ContainerId}",
            templates.Count, containerId);

        return templates;
    }

    public IQueryable<ContainerTemplate> GetQuery()
    {
        return context.ContainerTemplate
            .Include(t => t.Shift)
            .OrderBy(t => t.ContainerId)
            .ThenBy(t => t.Weekday)
            .ThenBy(t => t.IsHoliday)
            .AsNoTracking();
    }

    public async Task<List<Guid>> GetUsedShiftIds(Guid? excludeContainerId = null, CancellationToken cancellationToken = default)
    {
        var query = context.ContainerTemplateItem.AsQueryable();

        if (excludeContainerId.HasValue)
        {
            query = query.Where(cti => cti.ContainerTemplate.ContainerId != excludeContainerId.Value);
        }

        return await query
            .Select(cti => cti.ShiftId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ContainerTemplateItem>> GetItemsForTemplate(Guid templateId, bool tracked)
    {
        Logger.LogInformation("Fetching items for template: {TemplateId}, Tracked: {Tracked}", templateId, tracked);

        var query = context.ContainerTemplateItem
            .Where(i => i.ContainerTemplateId == templateId);

        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        var items = await query.ToListAsync();

        Logger.LogInformation("Found {Count} items for template: {TemplateId}", items.Count, templateId);
        return items;
    }

    public async Task UpdateItem(ContainerTemplateItemResource itemResource)
    {
        Logger.LogInformation("Updating ContainerTemplateItem: {ItemId}", itemResource.Id);

        var existingItem = await context.ContainerTemplateItem.FindAsync(itemResource.Id);

        if (existingItem == null)
        {
            Logger.LogWarning("ContainerTemplateItem not found for update: {ItemId}", itemResource.Id);
            throw new InvalidOperationException($"ContainerTemplateItem with ID {itemResource.Id} not found");
        }

        existingItem.ShiftId = itemResource.ShiftId;
        existingItem.StartShift = itemResource.StartShift;
        existingItem.EndShift = itemResource.EndShift;
        existingItem.BriefingTime = itemResource.BriefingTime;
        existingItem.DebriefingTime = itemResource.DebriefingTime;
        existingItem.TravelTimeAfter = itemResource.TravelTimeAfter;
        existingItem.TravelTimeBefore = itemResource.TravelTimeBefore;
        existingItem.TimeRangeStartShift = itemResource.TimeRangeStartShift;
        existingItem.TimeRangeEndShift = itemResource.TimeRangeEndShift;
        existingItem.TransportMode = itemResource.TransportMode;

        context.Entry(existingItem).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

        Logger.LogInformation("ContainerTemplateItem updated: {ItemId}", itemResource.Id);
    }

    public async Task<Guid> CreateItem(Guid templateId, ContainerTemplateItemResource itemResource)
    {
        Logger.LogInformation("Creating new ContainerTemplateItem for template: {TemplateId}", templateId);

        var newItem = new ContainerTemplateItem
        {
            ContainerTemplateId = templateId,
            ShiftId = itemResource.ShiftId,
            StartShift = itemResource.StartShift,
            EndShift = itemResource.EndShift,
            BriefingTime = itemResource.BriefingTime,
            DebriefingTime = itemResource.DebriefingTime,
            TravelTimeAfter = itemResource.TravelTimeAfter,
            TravelTimeBefore = itemResource.TravelTimeBefore,
            TimeRangeStartShift = itemResource.TimeRangeStartShift,
            TimeRangeEndShift = itemResource.TimeRangeEndShift,
            TransportMode = itemResource.TransportMode
        };

        await context.ContainerTemplateItem.AddAsync(newItem);

        Logger.LogInformation("ContainerTemplateItem created with ID: {ItemId}", newItem.Id);
        return newItem.Id;
    }

    public async Task DeleteItem(Guid itemId)
    {
        Logger.LogInformation("Deleting ContainerTemplateItem: {ItemId}", itemId);

        var item = await context.ContainerTemplateItem.FindAsync(itemId);

        if (item == null)
        {
            Logger.LogWarning("ContainerTemplateItem not found for deletion: {ItemId}", itemId);
            return;
        }

        context.ContainerTemplateItem.Remove(item);

        Logger.LogInformation("ContainerTemplateItem deleted: {ItemId}", itemId);
    }
}
