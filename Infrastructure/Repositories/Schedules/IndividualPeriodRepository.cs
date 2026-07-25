// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for IndividualPeriod, keeping the child Period rows in sync via
/// EntityCollectionUpdateService and cascading the soft delete to active Period children.
/// </summary>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class IndividualPeriodRepository : BaseRepository<IndividualPeriod>, IIndividualPeriodRepository
{
    private readonly EntityCollectionUpdateService _collectionUpdateService;

    public IndividualPeriodRepository(
        DataBaseContext context,
        ILogger<IndividualPeriod> logger,
        EntityCollectionUpdateService collectionUpdateService)
        : base(context, logger)
    {
        _collectionUpdateService = collectionUpdateService;
    }

    public override async Task Add(IndividualPeriod model)
    {
        foreach (var period in model.Periods)
        {
            period.IndividualPeriodId = model.Id;
        }

        await context.IndividualPeriod.AddAsync(model);
        Logger.LogInformation("IndividualPeriod added: {IndividualPeriodId}, Periods count: {Count}",
            model.Id, model.Periods.Count);
    }

    public override async Task<IndividualPeriod?> Get(Guid id)
    {
        return await context.IndividualPeriod
            .Include(ip => ip.Periods)
            .AsNoTracking()
            .FirstOrDefaultAsync(ip => ip.Id == id);
    }

    public override async Task<List<IndividualPeriod>> List()
    {
        return await context.IndividualPeriod
            .Include(ip => ip.Periods)
            .AsNoTracking()
            .ToListAsync();
    }

    public override async Task<IndividualPeriod?> Put(IndividualPeriod model)
    {
        var existing = await context.IndividualPeriod
            .Include(ip => ip.Periods)
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (existing == null)
        {
            Logger.LogWarning("IndividualPeriod not found for update: {IndividualPeriodId}", model.Id);
            return null;
        }

        existing.Name = model.Name;

        _collectionUpdateService.UpdateCollection(
            existing.Periods,
            model.Periods,
            existing.Id,
            (period, individualPeriodId) => period.IndividualPeriodId = individualPeriodId);

        Logger.LogInformation("IndividualPeriod updated: {IndividualPeriodId}, Periods count: {Count}",
            existing.Id, existing.Periods.Count);

        return existing;
    }

    public override async Task<IndividualPeriod?> Delete(Guid id)
    {
        var existing = await context.IndividualPeriod
            .Include(ip => ip.Periods)
            .FirstOrDefaultAsync(ip => ip.Id == id);

        if (existing == null)
        {
            return null;
        }

        foreach (var period in existing.Periods.Where(p => !p.IsDeleted))
        {
            context.Entry(period).State = EntityState.Deleted;
        }

        context.Remove(existing);

        Logger.LogInformation("IndividualPeriod deleted: {IndividualPeriodId}, Periods cascaded: {Count}",
            existing.Id, existing.Periods.Count);

        return existing;
    }
}
