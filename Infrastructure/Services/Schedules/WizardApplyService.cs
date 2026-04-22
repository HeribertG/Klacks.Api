// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardApplyService"/>. Reads the cached scenario,
/// materialises non-locked tokens as a <see cref="BulkAddWorksCommand"/> dispatched via mediator.
/// Routing through the existing Work-Create pipeline ensures PeriodHours, Collision-Pipeline,
/// Notifications and Container-Expansion side-effects fire correctly.
/// </summary>
/// <param name="resultCache">Cache populated by the wizard runner</param>
/// <param name="mediator">Mediator used to dispatch BulkAddWorksCommand</param>
public sealed class WizardApplyService : IWizardApplyService
{
    private readonly WizardResultCache _resultCache;
    private readonly IMediator _mediator;

    public WizardApplyService(WizardResultCache resultCache, IMediator mediator)
    {
        _resultCache = resultCache;
        _mediator = mediator;
    }

    public async Task<IReadOnlyList<Guid>> ApplyAsync(Guid jobId, CancellationToken ct)
    {
        if (!_resultCache.TryGet(jobId, out var scenario, out var analyseToken) || scenario is null)
        {
            throw new InvalidOperationException($"No cached wizard result for job id {jobId}.");
        }

        var items = scenario.Tokens
            .Where(t => !t.IsLocked)
            .Select(t => BuildBulkItem(t, analyseToken))
            .ToList();

        if (items.Count == 0)
        {
            _resultCache.Invalidate(jobId);
            return [];
        }

        var periodStart = items.Min(i => i.CurrentDate);
        var periodEnd = items.Max(i => i.CurrentDate);

        var response = await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
        {
            Works = items,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
        }));

        _resultCache.Invalidate(jobId);
        return response.CreatedIds;
    }

    private static BulkWorkItem BuildBulkItem(CoreToken token, Guid? analyseToken)
    {
        return new BulkWorkItem
        {
            ClientId = Guid.Parse(token.AgentId),
            ShiftId = token.ShiftRefId,
            CurrentDate = token.Date,
            StartTime = TimeOnly.FromDateTime(token.StartAt),
            EndTime = TimeOnly.FromDateTime(token.EndAt),
            WorkTime = token.TotalHours,
            Information = null,
            AnalyseToken = analyseToken,
        };
    }
}
