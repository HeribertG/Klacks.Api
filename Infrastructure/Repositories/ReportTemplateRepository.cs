using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class ReportTemplateRepository : IReportTemplateRepository
{
    private readonly DataBaseContext _context;

    public ReportTemplateRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReportTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReportTemplates
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
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

        _context.Entry(existing).CurrentValues.SetValues(template);
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
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
