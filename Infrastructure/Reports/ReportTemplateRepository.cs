using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Entities.Reports;

namespace Klacks.Api.Infrastructure.Reports;

public class ReportTemplateRepository : IReportTemplateRepository
{
    // Temporary in-memory storage until Phase 2 (Database implementation)
    private static readonly List<ReportTemplate> _templates = new();
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task<IEnumerable<ReportTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_templates.Where(t => !t.IsDeleted).AsEnumerable());
    }

    public Task<IEnumerable<ReportTemplate>> GetByTypeAsync(ReportType type, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_templates.Where(t => !t.IsDeleted && t.Type == type).AsEnumerable());
    }

    public Task<ReportTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = _templates.FirstOrDefault(t => t.Id == id && !t.IsDeleted);
        return Task.FromResult(template);
    }

    public async Task<ReportTemplate> CreateAsync(ReportTemplate template, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            template.Id = Guid.NewGuid();
            template.CreatedAt = DateTime.UtcNow;
            _templates.Add(template);
            return template;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ReportTemplate> UpdateAsync(ReportTemplate template, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var existing = _templates.FirstOrDefault(t => t.Id == template.Id);
            if (existing == null)
            {
                throw new ArgumentException($"Template with ID {template.Id} not found");
            }

            var index = _templates.IndexOf(existing);
            template.UpdatedAt = DateTime.UtcNow;
            _templates[index] = template;
            return template;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var template = _templates.FirstOrDefault(t => t.Id == id);
            if (template != null)
            {
                template.IsDeleted = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
