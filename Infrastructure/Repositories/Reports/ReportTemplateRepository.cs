// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Reports;

public class ReportTemplateRepository : IReportTemplateRepository
{
    private const int MaxVersions = 10;

    private readonly DataBaseContext _context;

    public ReportTemplateRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReportTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _context.ReportTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        // The version history is only served by GetByIdAsync so the list stays small.
        foreach (var template in templates)
        {
            template.Versions = [];
        }

        return templates;
    }

    public async Task<IEnumerable<ReportTemplate>> GetByTypeAsync(ReportType type, CancellationToken cancellationToken = default)
    {
        return await _context.ReportTemplates
            .Where(t => t.Type == type)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReportTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReportTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<ReportTemplate> CreateAsync(ReportTemplate template, CancellationToken cancellationToken = default)
    {
        _context.ReportTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<ReportTemplate> UpdateAsync(ReportTemplate template, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ReportTemplates.FindAsync(new object[] { template.Id }, cancellationToken);
        if (existing == null)
        {
            throw new ArgumentException($"Template with ID {template.Id} not found");
        }

        var history = BuildHistory(existing);

        _context.Entry(existing).CurrentValues.SetValues(template);
        existing.Sections = template.Sections;
        existing.PageSetup = template.PageSetup;
        existing.DataSetIds = template.DataSetIds;
        existing.Versions = history;
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    /// <summary>
    /// Keeps a snapshot of the state before the update, capped at the newest entries.
    /// </summary>
    /// <param name="existing">Template as currently stored</param>
    private static List<ReportTemplateVersion> BuildHistory(ReportTemplate existing)
    {
        var snapshot = new ReportTemplateVersion
        {
            SavedAt = DateTime.UtcNow,
            SavedBy = existing.CurrentUserUpdated ?? existing.CurrentUserCreated,
            Name = existing.Name,
            PageSetup = existing.PageSetup,
            Sections = existing.Sections,
        };

        var history = new List<ReportTemplateVersion>(existing.Versions ?? []) { snapshot };
        return history.Count > MaxVersions
            ? history.Skip(history.Count - MaxVersions).ToList()
            : history;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _context.ReportTemplates.FindAsync(new object[] { id }, cancellationToken);
        if (template != null)
        {
            _context.ReportTemplates.Remove(template);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
