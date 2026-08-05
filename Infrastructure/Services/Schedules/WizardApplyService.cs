// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardApplyService"/>. Reads the cached scenario,
/// partitions the non-locked tokens through the shared compliance guardrail and materialises the
/// accepted ones as a <see cref="BulkAddWorksCommand"/> dispatched via mediator. Routing through
/// the existing Work-Create pipeline ensures PeriodHours, Collision-Pipeline, Notifications and
/// Container-Expansion side-effects fire correctly.
/// </summary>
/// <param name="resultCache">Cache populated by the wizard runner</param>
/// <param name="mediator">Mediator used to dispatch BulkAddWorksCommand</param>
/// <param name="scenarioRepository">Repository for persisting new AnalyseScenario entities</param>
/// <param name="scenarioService">Service for cloning schedule data into the new scenario</param>
/// <param name="unitOfWork">Unit of work for flushing EF changes</param>
/// <param name="softeningRepository">Repository persisting per-cell softening escalations</param>
/// <param name="captureRepository">Repository persisting the run-protocol capture for the preference-learner</param>
/// <param name="partitionService">Shared accept/block compliance partition incl. the K1 supervisor override</param>
/// <param name="logger">Logger used for best-effort capture warnings</param>
public sealed class WizardApplyService : IWizardApplyService
{
    private readonly WizardResultCache _resultCache;
    private readonly IMediator _mediator;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkSofteningRepository _softeningRepository;
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly ICompliancePartitionService _partitionService;
    private readonly ILogger<WizardApplyService> _logger;

    public WizardApplyService(
        WizardResultCache resultCache,
        IMediator mediator,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWorkSofteningRepository softeningRepository,
        IWizardRunCaptureRepository captureRepository,
        ICompliancePartitionService partitionService,
        ILogger<WizardApplyService> logger)
    {
        _resultCache = resultCache;
        _mediator = mediator;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _softeningRepository = softeningRepository;
        _captureRepository = captureRepository;
        _partitionService = partitionService;
        _logger = logger;
    }

    public async Task<WizardApplyOutcome> ApplyAsync(Guid jobId, bool overrideBlock, CancellationToken ct)
    {
        if (!_resultCache.TryGet(jobId, out var scenario, out var analyseToken, out var escalations, out var subScoreJson, out var stage0Violations) || scenario is null)
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
            return new WizardApplyOutcome([], [], [], OverrideApplied: false);
        }

        // The direct path writes into the world the wizard ran on: the source scenario when the run
        // carried an analyse token, the real plan otherwise. The partition must check that same world.
        var rows = items
            .Select(i => new PlannedWorkRow(i.ClientId, i.CurrentDate, i.StartTime, i.EndTime, i.ShiftId))
            .ToList();
        var partition = await _partitionService.PartitionAsync(rows, analyseToken, overrideBlock, ct);

        var acceptedItems = partition.AcceptedIndexes.Select(i => items[i]).ToList();
        var skipped = BuildSkippedPlacements(rows, partition.BlockedIndexes);

        if (acceptedItems.Count == 0)
        {
            // Everything was blocked: keep the cached result alive so a supervisor can retry the
            // same jobId with an authorised override instead of re-running the whole GA.
            return new WizardApplyOutcome([], partition.ReportableConflicts, skipped, partition.OverrideApplied);
        }

        var periodStart = acceptedItems.Min(i => i.CurrentDate);
        var periodEnd = acceptedItems.Max(i => i.CurrentDate);

        var response = await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
        {
            Works = acceptedItems,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
        }));

        await PersistEscalationsAsync(acceptedItems, escalations, analyseToken, periodStart, periodEnd, ct);

        // Direct-apply is the most common acceptance case: the tokens go straight into the real plan without
        // a scenario. ChurnAtApply stays null (first-fill measures nothing); GroupId is unknown on this path.
        await CaptureRunAsync(
            jobId,
            WizardApplyKind.Direct,
            scenarioId: null,
            runGroupId: null,
            groupId: null,
            periodStart,
            periodEnd,
            subScoreJson,
            stage0Violations,
            response.CreatedIds,
            ct);

        _resultCache.Invalidate(jobId);
        return new WizardApplyOutcome(response.CreatedIds, partition.ReportableConflicts, skipped, partition.OverrideApplied);
    }

    public async Task<(AnalyseScenarioResource Scenario, WizardApplyOutcome Outcome)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        bool overrideBlock,
        CancellationToken ct,
        string? namePrefixOverride = null)
    {
        if (!_resultCache.TryGet(jobId, out var cachedScenario, out var sourceAnalyseToken, out var escalations, out var subScoreJson, out var stage0Violations) || cachedScenario is null)
        {
            throw new InvalidOperationException($"No cached wizard result for job id {jobId}.");
        }

        var runGroupId = await ResolveRunGroupIdAsync(sourceAnalyseToken, ct);

        var items = cachedScenario.Tokens
            .Where(t => !t.IsLocked)
            .ToList();

        var periodFrom = items.Count > 0
            ? items.Min(t => t.Date)
            : DateOnly.FromDateTime(DateTime.UtcNow);
        var periodUntil = items.Count > 0
            ? items.Max(t => t.Date)
            : periodFrom;

        var name = await GenerateUniqueNameAsync(periodFrom, periodUntil, groupId, ct, namePrefixOverride);
        var token = Guid.NewGuid();

        var analyseScenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = periodFrom,
            UntilDate = periodUntil,
            Token = token,
            RunGroupId = runGroupId,
        };

        await _scenarioRepository.Add(analyseScenario);
        var shiftIdMap = await _scenarioService.CloneScenarioDataAsync(groupId, periodFrom, periodUntil, token, additionalShiftIds: null, ct);
        await _unitOfWork.CompleteAsync();

        // The planner REPLACES the incumbent on exactly the (shift, date) slots it fills. Remove the cloned
        // movable works on those slots before materialising the planner works, so an accepted plan does not
        // double-book them (grill H2). Slots the planner did not plan keep their cloned work (the run can
        // cover a narrower agent/shift set than the group-scoped clone); locks/breaks are always preserved.
        var plannedSlots = items
            .Select(t => (ShiftId: shiftIdMap.TryGetValue(t.ShiftRefId, out var mapped) ? mapped : t.ShiftRefId, t.Date))
            .ToHashSet();
        await _scenarioService.SoftDeleteClonedWorksOnSlotsAsync(token, periodFrom, periodUntil, plannedSlots, ct);
        await _unitOfWork.CompleteAsync();

        // Partition AFTER the slot soft-delete has been flushed: the guardrail reads the DB via
        // AsNoTracking under the NEW scenario token, so it must see the clone world exactly as the
        // planner works will land in it (unflushed changes would make the check silently pass or
        // report phantom collisions with the removed incumbents).
        var rows = items
            .Select(t => new PlannedWorkRow(
                Guid.Parse(t.AgentId),
                t.Date,
                TimeOnly.FromDateTime(t.StartAt),
                TimeOnly.FromDateTime(t.EndAt),
                t.ShiftRefId))
            .ToList();
        var partition = await _partitionService.PartitionAsync(rows, token, overrideBlock, ct);

        var acceptedTokens = partition.AcceptedIndexes.Select(i => items[i]).ToList();
        var skipped = BuildSkippedPlacements(rows, partition.BlockedIndexes);

        var bulkItems = acceptedTokens
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

        await CaptureRunAsync(
            jobId,
            WizardApplyKind.Scenario,
            scenarioId: analyseScenario.Id,
            runGroupId: runGroupId,
            groupId: groupId,
            periodFrom,
            periodUntil,
            subScoreJson,
            stage0Violations,
            createdIds,
            ct);

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
            RunGroupId = analyseScenario.RunGroupId,
            CreatedByUser = analyseScenario.CreatedByUser,
            Status = (int)analyseScenario.Status,
        };

        return (resource, new WizardApplyOutcome(createdIds, partition.ReportableConflicts, skipped, partition.OverrideApplied));
    }

    private static List<SkippedPlacementDto> BuildSkippedPlacements(
        IReadOnlyList<PlannedWorkRow> rows,
        IReadOnlyList<BlockedRow> blockedRows)
        => blockedRows
            .Select(b => new SkippedPlacementDto(
                rows[b.Index].ClientId,
                rows[b.Index].Date,
                rows[b.Index].ShiftId,
                b.ReasonKey))
            .ToList();

    private async Task<Guid> ResolveRunGroupIdAsync(Guid? sourceAnalyseToken, CancellationToken ct)
    {
        if (sourceAnalyseToken is not Guid token)
        {
            return Guid.NewGuid();
        }
        var sourceScenario = await _scenarioRepository.GetByTokenAsync(token, ct);
        return sourceScenario?.RunGroupId ?? Guid.NewGuid();
    }

    private async Task<string> GenerateUniqueNameAsync(
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        CancellationToken ct,
        string? namePrefixOverride = null)
    {
        var prefix = string.IsNullOrWhiteSpace(namePrefixOverride) ? "Plan" : namePrefixOverride;
        var baseName = $"{prefix} {from:dd.MM.yy} – {until:dd.MM.yy}";
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

    /// <summary>
    /// Best-effort run-protocol capture for the (deferred) preference-learner. A capture failure must never
    /// break the apply — the planner's plan is more important than the measurement — so all faults are
    /// swallowed with a warning. The created work ids are the proposal set the measurement job later joins on.
    /// <para>
    /// ChurnAtApply is intentionally left null for BOTH Wizard-1 apply paths (direct and scenario). Wizard 1 is
    /// the first-fill planner: it creates the initial assignment rather than reworking an incumbent, so an
    /// apply-time edit-distance against the (empty) baseline is ~1.0 and measures nothing. Writing that pseudo
    /// value would only pollute the learner signal, hence null. The meaningful learner label is the deferred,
    /// event-cleaned CorrectionChurn, not ChurnAtApply.
    /// </para>
    /// </summary>
    private async Task CaptureRunAsync(
        Guid jobId,
        WizardApplyKind applyKind,
        Guid? scenarioId,
        Guid? runGroupId,
        Guid? groupId,
        DateOnly periodFrom,
        DateOnly periodUntil,
        string subScoreJson,
        int stage0Violations,
        IReadOnlyList<Guid> createdWorkIds,
        CancellationToken ct)
    {
        if (createdWorkIds.Count == 0)
        {
            return;
        }

        try
        {
            var capture = new WizardRunCapture
            {
                Id = Guid.NewGuid(),
                Engine = WizardEngine.TokenEvolution,
                JobId = jobId,
                RunGroupId = runGroupId,
                ScenarioId = scenarioId,
                ApplyKind = applyKind,
                GroupId = groupId,
                PeriodFrom = periodFrom,
                PeriodUntil = periodUntil,
                SubScoreJson = subScoreJson,
                Stage0Violations = stage0Violations,
                ChurnAtApply = null,
            };

            await _captureRepository.AddAsync(capture, createdWorkIds, ct);

            if (applyKind == WizardApplyKind.Direct)
            {
                var superseded = await _captureRepository.SupersedeOpenDirectCapturesAsync(
                    capture.Id, WizardEngine.TokenEvolution, periodFrom, periodUntil, ct);

                if (superseded > 0)
                {
                    _logger.LogInformation(
                        "Direct apply for job {JobId} superseded {Count} still-open direct capture(s) overlapping {From}..{Until}.",
                        jobId, superseded, periodFrom, periodUntil);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WizardRunCapture write failed for job {JobId} ({ApplyKind}); apply itself is unaffected", jobId, applyKind);
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
            Surcharges = token.Surcharges,
            Information = null,
            AnalyseToken = analyseToken,
        };
    }
}
