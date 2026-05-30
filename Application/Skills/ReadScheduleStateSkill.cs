// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the current schedule grid of a planning blade (group + period): which employee is
/// assigned to which shift on which day, plus lock level and group-restriction flags. This is
/// the foundation skill that makes Klacksy "see" the plan before reasoning about gaps, conflicts
/// or replacements. When an analyseToken is supplied the scenario view is read in isolation.
/// </summary>
/// <param name="groupId">Required. UUID of the group / planning blade.</param>
/// <param name="fromDate">Required. ISO date yyyy-MM-dd (period start).</param>
/// <param name="untilDate">Required. ISO date yyyy-MM-dd (period end, inclusive).</param>
/// <param name="analyseToken">Optional. UUID of a scenario; when set the isolated scenario grid is read instead of the real plan.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("read_schedule_state")]
public class ReadScheduleStateSkill : BaseSkillImplementation
{
    private const int MaxEntries = 750;

    private readonly IScheduleEntriesService _scheduleEntriesService;

    public ReadScheduleStateSkill(IScheduleEntriesService scheduleEntriesService)
    {
        _scheduleEntriesService = scheduleEntriesService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");
        var fromStr = GetRequiredString(parameters, "fromDate");
        var untilStr = GetRequiredString(parameters, "untilDate");
        var analyseTokenStr = GetParameter<string>(parameters, "analyseToken");

        if (!DateOnly.TryParse(fromStr, out var fromDate))
        {
            return SkillResult.Error($"Invalid fromDate: {fromStr}.");
        }
        if (!DateOnly.TryParse(untilStr, out var untilDate))
        {
            return SkillResult.Error($"Invalid untilDate: {untilStr}.");
        }
        if (untilDate < fromDate)
        {
            return SkillResult.Error("untilDate must be on or after fromDate.");
        }

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenStr))
        {
            if (!Guid.TryParse(analyseTokenStr, out var parsedToken))
            {
                return SkillResult.Error($"Invalid analyseToken: {analyseTokenStr}.");
            }
            analyseToken = parsedToken;
        }

        var cells = await _scheduleEntriesService
            .GetScheduleEntriesQuery(fromDate, untilDate, new List<Guid> { groupId }, analyseToken)
            .ToListAsync(cancellationToken);

        var ordered = cells
            .OrderBy(c => c.EntryDate)
            .ThenBy(c => c.StartTime)
            .ToList();

        var truncated = ordered.Count > MaxEntries;
        if (truncated)
        {
            ordered = ordered.Take(MaxEntries).ToList();
        }

        var entries = ordered.Select(ToEntry).ToList();
        var distinctEmployees = ordered.Select(c => c.ClientId).Distinct().Count();

        var data = new
        {
            GroupId = groupId,
            FromDate = fromDate.ToString("yyyy-MM-dd"),
            UntilDate = untilDate.ToString("yyyy-MM-dd"),
            AnalyseToken = analyseToken,
            IsScenario = analyseToken.HasValue,
            EntryCount = entries.Count,
            DistinctEmployees = distinctEmployees,
            Truncated = truncated,
            Entries = entries
        };

        var scenarioNote = analyseToken.HasValue ? " (scenario view)" : string.Empty;
        var truncatedNote = truncated ? $" Result truncated to {MaxEntries} entries." : string.Empty;
        var message =
            $"Schedule state for group {groupId} between {fromDate:yyyy-MM-dd} and {untilDate:yyyy-MM-dd}{scenarioNote}: " +
            $"{entries.Count} entr{(entries.Count == 1 ? "y" : "ies")}, {distinctEmployees} employee(s).{truncatedNote}";

        return SkillResult.SuccessResult(data, message);
    }

    private static object ToEntry(ScheduleCell cell) => new
    {
        cell.ClientId,
        Date = DateOnly.FromDateTime(cell.EntryDate).ToString("yyyy-MM-dd"),
        EntryType = ResolveEnumName<ScheduleEntryType>(cell.EntryType),
        Shift = cell.EntryName,
        cell.Abbreviation,
        StartTime = cell.StartTime.ToString(@"hh\:mm"),
        EndTime = cell.EndTime.ToString(@"hh\:mm"),
        LockLevel = ResolveEnumName<WorkLockLevel>(cell.LockLevel),
        IsLocked = cell.LockLevel != (int)WorkLockLevel.None,
        cell.IsGroupRestricted,
        IsReplacement = cell.IsReplacementEntry,
        cell.ReplaceClientId
    };

    private static string ResolveEnumName<TEnum>(int value) where TEnum : struct, Enum
        => Enum.IsDefined(typeof(TEnum), value)
            ? ((TEnum)(object)value).ToString()
            : value.ToString();
}
