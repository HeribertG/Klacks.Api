// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="EvaluateScenarioQuery"/>. Resolves the scenario, then derives two
/// dimensions purely from persisted data so the result is independent of the transient wizard
/// cache: (1) rule-compliance via <see cref="IPeriodValidationLoader"/> in scenario mode, and
/// (2) the change-set by diffing the resolved schedule grid (real vs. scenario) returned by
/// <see cref="IScheduleEntriesService"/>. The grid already folds works, replacements and breaks
/// into effective cells, so the diff is clone-safe and producer-agnostic (propose_plan works,
/// cover_absence replacements and absences all surface).
/// </summary>
/// <param name="scenarioRepository">Resolves the scenario by token or id</param>
/// <param name="scheduleEntriesService">Reads the effective grid (real and scenario) for the diff</param>
/// <param name="validationLoader">Runs the rule-compliance checks in scenario mode</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class EvaluateScenarioQueryHandler
    : IRequestHandler<EvaluateScenarioQuery, ScenarioEvaluationResult>
{
    private const int MaxConflicts = 200;
    private const int MaxChangeEntries = 150;

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IPeriodValidationLoader _validationLoader;

    public EvaluateScenarioQueryHandler(
        IAnalyseScenarioRepository scenarioRepository,
        IScheduleEntriesService scheduleEntriesService,
        IPeriodValidationLoader validationLoader)
    {
        _scenarioRepository = scenarioRepository;
        _scheduleEntriesService = scheduleEntriesService;
        _validationLoader = validationLoader;
    }

    public async Task<ScenarioEvaluationResult> Handle(
        EvaluateScenarioQuery request,
        CancellationToken cancellationToken)
    {
        var scenario = await ResolveScenarioAsync(request, cancellationToken);
        if (scenario is null)
        {
            return ScenarioEvaluationResult.NotFound();
        }

        var groupIds = scenario.GroupId.HasValue
            ? new List<Guid> { scenario.GroupId.Value }
            : null;

        var issues = await _validationLoader.LoadAsync(
            scenario.FromDate, scenario.UntilDate, scenario.GroupId, scenario.Token, cancellationToken);

        var realCells = await _scheduleEntriesService
            .GetScheduleEntriesQuery(scenario.FromDate, scenario.UntilDate, groupIds, null)
            .ToListAsync(cancellationToken);
        var scenarioCells = await _scheduleEntriesService
            .GetScheduleEntriesQuery(scenario.FromDate, scenario.UntilDate, groupIds, scenario.Token)
            .ToListAsync(cancellationToken);

        var added = DiffCells(scenarioCells, realCells);
        var removed = DiffCells(realCells, scenarioCells);

        return Build(scenario, issues, realCells.Count, scenarioCells.Count, added, removed);
    }

    private async Task<AnalyseScenario?> ResolveScenarioAsync(
        EvaluateScenarioQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Token.HasValue)
        {
            return await _scenarioRepository.GetByTokenAsync(request.Token.Value, cancellationToken);
        }
        if (request.ScenarioId.HasValue)
        {
            return await _scenarioRepository.Get(request.ScenarioId.Value);
        }
        return null;
    }

    private static List<ScheduleCell> DiffCells(
        IReadOnlyList<ScheduleCell> source,
        IReadOnlyList<ScheduleCell> baseline)
    {
        var baselineCounts = new Dictionary<string, int>();
        foreach (var cell in baseline)
        {
            var key = BuildKey(cell);
            baselineCounts[key] = baselineCounts.TryGetValue(key, out var c) ? c + 1 : 1;
        }

        var result = new List<ScheduleCell>();
        foreach (var cell in source)
        {
            var key = BuildKey(cell);
            if (baselineCounts.TryGetValue(key, out var remaining) && remaining > 0)
            {
                baselineCounts[key] = remaining - 1;
                continue;
            }
            result.Add(cell);
        }
        return result;
    }

    private static string BuildKey(ScheduleCell cell) =>
        string.Join('|',
            cell.EntryType,
            cell.ClientId,
            cell.ReplaceClientId?.ToString() ?? string.Empty,
            cell.EntryName ?? string.Empty,
            cell.EntryDate.ToString("yyyyMMdd"),
            cell.StartTime,
            cell.EndTime);

    private static ScenarioEvaluationResult Build(
        AnalyseScenario scenario,
        List<DTOs.PeriodClosing.PeriodIssueDto> issues,
        int realEntryCount,
        int scenarioEntryCount,
        List<ScheduleCell> added,
        List<ScheduleCell> removed)
    {
        var errors = issues.Count(i => i.Severity == ScheduleValidationType.Error);
        var warnings = issues.Count(i => i.Severity == ScheduleValidationType.Warning);
        var info = issues.Count(i => i.Severity == ScheduleValidationType.Info);
        var byCode = issues
            .GroupBy(i => i.Code)
            .ToDictionary(g => g.Key, g => g.Count());

        var conflicts = issues
            .Take(MaxConflicts)
            .Select(i => new ScenarioConflictDto(
                i.ClientId,
                i.ClientName,
                i.Date.ToString("yyyy-MM-dd"),
                i.Severity.ToString(),
                i.Code,
                i.MessageKey,
                i.MessageParams))
            .ToList();

        var addedWork = added.Count(c => c.EntryType == (int)ScheduleEntryType.Work);
        var addedReplacement = added.Count(c => c.IsReplacementEntry);
        var addedBreak = added.Count(c => c.EntryType == (int)ScheduleEntryType.Break);
        var addedByType = added
            .GroupBy(c => EntryTypeName(c.EntryType))
            .ToDictionary(g => g.Key, g => g.Count());

        var changesTruncated = added.Count > MaxChangeEntries || removed.Count > MaxChangeEntries;
        var ruleCompliant = errors == 0;

        return new ScenarioEvaluationResult(
            Found: true,
            ScenarioId: scenario.Id,
            Token: scenario.Token,
            Name: scenario.Name,
            Status: scenario.Status.ToString(),
            GroupId: scenario.GroupId,
            FromDate: scenario.FromDate.ToString("yyyy-MM-dd"),
            UntilDate: scenario.UntilDate.ToString("yyyy-MM-dd"),
            TotalConflicts: issues.Count,
            Errors: errors,
            Warnings: warnings,
            Info: info,
            RuleCompliant: ruleCompliant,
            ByCode: byCode,
            Conflicts: conflicts,
            ConflictsTruncated: issues.Count > MaxConflicts,
            RealEntryCount: realEntryCount,
            ScenarioEntryCount: scenarioEntryCount,
            AddedEntryCount: added.Count,
            RemovedEntryCount: removed.Count,
            AddedWorkEntries: addedWork,
            AddedReplacementEntries: addedReplacement,
            AddedBreakEntries: addedBreak,
            AddedByType: addedByType,
            AddedEntries: added.Take(MaxChangeEntries).Select(ToChangeEntry).ToList(),
            RemovedEntries: removed.Take(MaxChangeEntries).Select(ToChangeEntry).ToList(),
            ChangesTruncated: changesTruncated,
            Recommendation: BuildRecommendation(errors, warnings, added.Count));
    }

    private static ScenarioChangeEntry ToChangeEntry(ScheduleCell cell) => new(
        EntryType: EntryTypeName(cell.EntryType),
        ClientId: cell.ClientId,
        ReplaceClientId: cell.ReplaceClientId,
        Shift: cell.EntryName,
        Date: DateOnly.FromDateTime(cell.EntryDate).ToString("yyyy-MM-dd"),
        StartTime: cell.StartTime.ToString(@"hh\:mm"),
        EndTime: cell.EndTime.ToString(@"hh\:mm"),
        IsReplacement: cell.IsReplacementEntry);

    private static string EntryTypeName(int value) =>
        Enum.IsDefined(typeof(ScheduleEntryType), value)
            ? ((ScheduleEntryType)value).ToString()
            : value.ToString();

    private static string BuildRecommendation(int errors, int warnings, int addedCount)
    {
        const string manualNote = " Accepting is a manual step (accept_scenario).";
        if (errors > 0)
        {
            return $"Scenario introduces {errors} rule error(s) (e.g. collisions) — do not accept as-is; " +
                   $"review and adjust first.{manualNote}";
        }
        if (warnings > 0)
        {
            return $"Scenario has no hard errors but {warnings} warning(s) (rest time / overtime). " +
                   $"It is rule-compliant on hard limits; review the warnings, then decide.{manualNote}";
        }
        if (addedCount == 0)
        {
            return $"Scenario is rule-clean but changes nothing versus the real plan.{manualNote}";
        }
        return $"Scenario is rule-clean and introduces {addedCount} change(s).{manualNote}";
    }
}
