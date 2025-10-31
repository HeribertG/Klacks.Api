using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class ContainerTemplateRepository : BaseRepository<ContainerTemplate>, IContainerTemplateRepository
{
    private readonly DataBaseContext context;
    private readonly EntityCollectionUpdateService _collectionUpdateService;

    public ContainerTemplateRepository(
        DataBaseContext context,
        ILogger<ContainerTemplate> logger,
        EntityCollectionUpdateService collectionUpdateService)
        : base(context, logger)
    {
        this.context = context;
        _collectionUpdateService = collectionUpdateService;
    }

    public new async Task Add(ContainerTemplate template)
    {
        foreach (var item in template.Items)
        {
            item.ContainerTemplateId = template.Id;
        }

        context.ContainerTemplate.Add(template);
        Logger.LogInformation("ContainerTemplate added: {TemplateId}, Items count: {Count}",
            template.Id, template.Items.Count);
    }

    public new async Task<ContainerTemplate?> Put(ContainerTemplate template)
    {
        var existingTemplate = await context.ContainerTemplate
            .Include(t => t.Shift)
            .Include(t => t.Items)
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
            existingTemplate.Items,
            template.Items,
            existingTemplate.Id,
            (item, templateId) => item.ContainerTemplateId = templateId);

        Logger.LogInformation("ContainerTemplate updated: {TemplateId}, Items count: {Count}",
            template.Id, existingTemplate.Items.Count);

        return existingTemplate;
    }

    public new async Task<ContainerTemplate?> Get(Guid id)
    {
        Logger.LogInformation("Fetching ContainerTemplate with ID: {TemplateId}", id);

        var template = await context.ContainerTemplate
            .Where(t => t.Id == id)
            .Include(t => t.Shift)
            .Include(t => t.Items)
                .ThenInclude(i => i.Shift)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (template != null)
        {
            Logger.LogInformation("ContainerTemplate with ID: {TemplateId} found with {ItemsCount} items.",
                id, template.Items.Count);
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
            .Include(t => t.Items)
                .ThenInclude(i => i.Shift)
            .AsSplitQuery()
            .AsNoTracking()
            .OrderBy(t => t.Weekday)
            .ThenBy(t => t.IsHoliday)
            .ToListAsync();

        Logger.LogInformation("Found {Count} templates for Container: {ContainerId}",
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
}
