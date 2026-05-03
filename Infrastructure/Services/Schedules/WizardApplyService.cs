// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
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
/// <param name="scenarioRepository">Repository for persisting new AnalyseScenario entities</param>
/// <param name="scenarioService">Service for cloning schedule data into the new scenario</param>
/// <param name="unitOfWork">Unit of work for flushing EF changes</param>
public sealed class WizardApplyService : IWizardApplyService
{
    private readonly WizardResultCache _resultCache;
    private readonly IMediator _mediator;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkSofteningRepository _softeningRepository;

    public WizardApplyService(
        WizardResultCache resultCache,
        IMediator mediator,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWorkSofteningRepository softeningRepository)
    {
        _resultCache = resultCache;
        _mediator = mediator;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _softeningRepository = softeningRepository;
    }

    public async Task<IReadOnlyList<Guid>> ApplyAsync(Guid jobId, CancellationToken ct)
    {
        if (!_resultCache.TryGet(jobId, out var scenario, out var analyseToken, out var escalations) || scenario is null)
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

        await PersistEscalationsAsync(items, escalations, analyseToken, periodStart, periodEnd, ct);

        _resultCache.Invalidate(jobId);
        return response.CreatedIds;
    }

    public async Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        CancellationToken ct)
    {
        if (!_resultCache.TryGet(jobId, out var cachedScenario, out _, out var escalations) || cachedScenario is null)
        {
            throw new InvalidOperationException($"No cached wizard result for job id {jobId}.");
        }

        var items = cachedScenario.Tokens
            .Where(t => !t.IsLocked)
            .ToList();

        var periodFrom = items.Count > 0
            ? items.Min(t => t.Date)
            : DateOnly.FromDateTime(DateTime.UtcNow);
        var periodUntil = items.Count > 0
            ? items.Max(t => t.Date)
            : periodFrom;

        var name = await GenerateUniqueNameAsync(periodFrom, periodUntil, groupId, ct);
        var token = Guid.NewGuid();

        var analyseScenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = periodFrom,
            UntilDate = periodUntil,
            Token = token,
        };

        await _scenarioRepository.Add(analyseScenario);
        var shiftIdMap = await _scenarioService.CloneScenarioDataAsync(groupId, periodFrom, periodUntil, token, ct);
        await _unitOfWork.CompleteAsync();

        var bulkItems = items
            .Select(t => BuildBulkItem(t, token, shiftIdMap))
            .ToList();

        IReadOnlyList<Guid> createdIds = [];

        if (bulkItems.Count > 0)
        {
            var response = await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
            {
                Works = bulkItems,
                PeriodStart = periodFrom,
                PeriodEnd = periodUntil,
            }));
            createdIds = response.CreatedIds;
        }

        await PersistEscalationsAsync(bulkItems, escalations, token, periodFrom, periodUntil, ct);

        _resultCache.Invalidate(jobId);

        var resource = new AnalyseScenarioResource
        {
            Id = analyseScenario.Id,
            Name = analyseScenario.Name,
            Description = analyseScenario.Description,
            GroupId = analyseScenario.GroupId,
            FromDate = analyseScenario.FromDate,
            UntilDate = analyseScenario.UntilDate,
            Token = analyseScenario.Token,
            CreatedByUser = analyseScenario.CreatedByUser,
            Status = (int)analyseScenario.Status,
        };

        return (resource, createdIds);
    }

    private async Task<string> GenerateUniqueNameAsync(
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        CancellationToken ct)
    {
        var baseName = $"Plan {from:dd.MM.yy} – {until:dd.MM.yy}";
        var existing = await _scenarioRepository.GetByGroupAsync(groupId, ct);
        var existingNames = existing.Select(s => s.Name).ToHashSet();

        if (!existingNames.Contains(baseName))
        {
            return baseName;
        }

        var counter = 2;
        while (true)
        {
            var candidate = $"{baseName} ({counter})";
            if (!existingNames.Contains(candidate))
            {
                return candidate;
            }
            counter++;
        }
    }

    private async Task PersistEscalationsAsync(
        IReadOnlyList<BulkWorkItem> appliedWorks,
        IReadOnlyList<WizardEscalationDto> escalations,
        Guid? analyseToken,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken ct)
    {
        var agentIds = appliedWorks.Select(w => w.ClientId).Distinct().ToList();
        var newRows = MapEscalations(escalations, analyseToken, agentIds, periodStart, periodEnd);
        await _softeningRepository.ReplaceForRangeAsync(agentIds, periodStart, periodEnd, analyseToken, newRows, ct);
        await _unitOfWork.CompleteAsync();
    }

    private static List<WorkSoftening> MapEscalations(
        IReadOnlyList<WizardEscalationDto> escalations,
        Guid? analyseToken,
        IReadOnlyList<Guid> appliedAgentIds,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var rows = new List<WorkSoftening>(escalations.Count);
        var validAgents = new HashSet<Guid>(appliedAgentIds);
        foreach (var esc in escalations)
        {
            if (!Guid.TryParse(esc.AgentId, out var agentId) || !validAgents.Contains(agentId))
            {
                continue;
            }
            if (!DateOnly.TryParse(esc.Date, out var date) || date < periodStart || date > periodEnd)
            {
                continue;
            }
            rows.Add(new WorkSoftening
            {
                ClientId = agentId,
                CurrentDate = date,
                Kind = MapKind(esc.RuleName),
                RuleName = esc.RuleName,
                Hint = esc.Hint,
                AnalyseToken = analyseToken,
            });
        }
        return rows;
    }

    private static SofteningKind MapKind(string ruleName)
    {
        return ruleName switch
        {
            "MinRestDays" => SofteningKind.MinRestDays,
            "MaxWorkDays" or "MaxConsecutiveDays" => SofteningKind.MaxConsecutiveWorkDays,
            "PreferredShift" or "Preferred" => SofteningKind.PreferredShiftViolation,
            "Blacklist" or "BlacklistedShift" => SofteningKind.BlacklistedShiftAssigned,
            "WeeklyHours" or "HeavyWeek" => SofteningKind.HeavyWeeklyLoad,
            _ => SofteningKind.Unknown,
        };
    }

    private static BulkWorkItem BuildBulkItem(CoreToken token, Guid? analyseToken, IReadOnlyDictionary<Guid, Guid>? shiftIdMap = null)
    {
        var shiftId = shiftIdMap != null && shiftIdMap.TryGetValue(token.ShiftRefId, out var mapped)
            ? mapped
            : token.ShiftRefId;

        return new BulkWorkItem
        {
            ClientId = Guid.Parse(token.AgentId),
            ShiftId = shiftId,
            CurrentDate = token.Date,
            StartTime = TimeOnly.FromDateTime(token.StartAt),
            EndTime = TimeOnly.FromDateTime(token.EndAt),
            WorkTime = token.TotalHours,
            Information = null,
            AnalyseToken = analyseToken,
        };
    }
}
