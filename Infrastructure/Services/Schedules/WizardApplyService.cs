// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardApplyService"/>. Reads the cached scenario,
/// materialises non-locked tokens as Work rows (locked tokens already exist in the DB) and saves changes.
/// </summary>
/// <param name="resultCache">Cache populated by the wizard runner</param>
/// <param name="workRepository">Repository used to persist the new work entries</param>
/// <param name="dbContext">Context to commit the changes</param>
public sealed class WizardApplyService : IWizardApplyService
{
    private readonly WizardResultCache _resultCache;
    private readonly IWorkRepository _workRepository;
    private readonly DataBaseContext _dbContext;

    public WizardApplyService(
        WizardResultCache resultCache,
        IWorkRepository workRepository,
        DataBaseContext dbContext)
    {
        _resultCache = resultCache;
        _workRepository = workRepository;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> ApplyAsync(Guid jobId, CancellationToken ct)
    {
        if (!_resultCache.TryGet(jobId, out var scenario) || scenario is null)
        {
            throw new InvalidOperationException($"No cached wizard result for job id {jobId}.");
        }

        var created = new List<Guid>();
        foreach (var token in scenario.Tokens)
        {
            if (token.IsLocked)
            {
                continue;
            }

            ct.ThrowIfCancellationRequested();

            var work = BuildWork(token);
            await _workRepository.Add(work);
            created.Add(work.Id);
        }

        await _dbContext.SaveChangesAsync(ct);
        _resultCache.Invalidate(jobId);
        return created;
    }

    private static Work BuildWork(CoreToken token)
    {
        return new Work
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.Parse(token.AgentId),
            ShiftId = token.ShiftRefId,
            CurrentDate = token.Date,
            StartTime = TimeOnly.FromDateTime(token.StartAt),
            EndTime = TimeOnly.FromDateTime(token.EndAt),
            WorkTime = token.TotalHours,
            AnalyseToken = null,
        };
    }
}
