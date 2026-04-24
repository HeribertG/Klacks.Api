// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * EF Core implementation of IWizardTrainingRepository.
 * Does not call SaveChanges itself — the caller is expected to commit via IUnitOfWork
 * when part of a larger transaction, or to call the dedicated persist helper on the service.
 * @param context - The shared EF Core database context
 */

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public sealed class WizardTrainingRepository : IWizardTrainingRepository
{
    private readonly DataBaseContext _context;

    public WizardTrainingRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WizardTrainingRun run, CancellationToken ct)
    {
        await _context.WizardTrainingRuns.AddAsync(run, ct);
    }

    public async Task<IReadOnlyList<WizardTrainingRun>> GetRecentAsync(int limit, CancellationToken ct)
    {
        return await _context.WizardTrainingRuns
            .AsNoTracking()
            .OrderByDescending(r => r.CreateTime)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<WizardTrainingRun?> GetBestAsync(CancellationToken ct)
    {
        return await _context.WizardTrainingRuns
            .AsNoTracking()
            .Where(r => r.Stage0Violations == 0)
            .OrderByDescending(r => r.Stage2Score)
            .ThenBy(r => r.DurationMs)
            .FirstOrDefaultAsync(ct);
    }
}
