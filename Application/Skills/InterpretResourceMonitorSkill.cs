// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads and INTERPRETS the resource monitor (capacity vs. demand over a year) plus the
/// current-month shift-coverage statistics so Klacksy can judge where a group is over- or
/// understaffed, which months are critical, and whether covering an absence is even worth it.
/// This is the aggregate view (capacity vs. demand over time), complementary to the grid-detail
/// of read_schedule_state. Demand = scheduled shifts (DienstCount); capacity bands = desired
/// (WunschCount) and maximum (MaxCount) daily readiness; all on the same headcount axis.
/// Note: the shift-coverage figures are always for the current month regardless of the year.
/// </summary>
/// <param name="year">Required. Calendar year to interpret (e.g. 2026).</param>
/// <param name="groupId">Optional. UUID of a group to focus on; null means all employees.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("interpret_resource_monitor")]
public class InterpretResourceMonitorSkill : BaseSkillImplementation
{
    private const int MinReasonableYear = 2000;
    private const int MaxReasonableYear = 3000;

    private readonly IMediator _mediator;

    public InterpretResourceMonitorSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var year = GetRequiredInt(parameters, "year");
        if (year < MinReasonableYear || year > MaxReasonableYear)
        {
            return SkillResult.Error($"Invalid year: {year}.");
        }

        var groupIdStr = GetParameter<string>(parameters, "groupId");
        Guid? groupId = null;
        if (!string.IsNullOrWhiteSpace(groupIdStr))
        {
            if (!Guid.TryParse(groupIdStr, out var parsedGroup))
            {
                return SkillResult.Error($"Invalid groupId: {groupIdStr}.");
            }
            groupId = parsedGroup;
        }

        var monitor = await _mediator.Send(new GetResourceMonitorQuery(year, groupId), cancellationToken);
        var days = monitor.DailyData?.ToList() ?? [];

        if (days.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { Year = year, GroupId = groupId, TotalDays = 0, Months = Array.Empty<object>() },
                $"No resource-monitor data for {year}{GroupNote(groupId)}.");
        }

        var months = days
            .GroupBy(d => d.Date.Month)
            .Select(g => new
            {
                Month = g.Key,
                Days = g.Count(),
                UnderstaffedDays = g.Count(d => d.DienstCount > d.MaxCount),
                TightDays = g.Count(d => d.DienstCount > d.WunschCount && d.DienstCount <= d.MaxCount),
                AvgDemand = Math.Round(g.Average(d => d.DienstCount), 1),
                AvgMaxCapacity = Math.Round(g.Average(d => d.MaxCount), 1),
                AvgDesiredCapacity = Math.Round(g.Average(d => d.WunschCount), 1),
                AvgHeadcount = Math.Round(g.Average(d => d.TotalCount), 1),
                AvgAbsence = Math.Round(g.Average(d => d.AbsenzCount), 1)
            })
            .OrderBy(m => m.Month)
            .ToList();

        var totalUnderstaffedDays = days.Count(d => d.DienstCount > d.MaxCount);
        var totalTightDays = days.Count(d => d.DienstCount > d.WunschCount && d.DienstCount <= d.MaxCount);
        var criticalMonths = months
            .Where(m => m.UnderstaffedDays > 0)
            .OrderByDescending(m => m.UnderstaffedDays)
            .Select(m => m.Month)
            .ToList();

        var coverage = await _mediator.Send(new GetShiftCoverageStatisticsQuery(), cancellationToken);
        var coverageList = coverage?.ToList() ?? [];
        var relevantCoverage = groupId.HasValue
            ? coverageList.Where(c => c.GroupId == groupId.Value).ToList()
            : coverageList;

        var currentMonthCoverage = relevantCoverage.Select(c => new
        {
            c.GroupId,
            c.GroupName,
            c.TotalSlots,
            c.CoveredSlots,
            UncoveredSlots = c.TotalSlots - c.CoveredSlots,
            CoverageRatio = c.TotalSlots > 0
                ? Math.Round((double)c.CoveredSlots / c.TotalSlots, 2)
                : (double?)null,
            c.SealedWorkEntries,
            c.TotalWorkEntries
        }).ToList();

        var data = new
        {
            Year = year,
            GroupId = groupId,
            TotalDays = days.Count,
            UnderstaffedDays = totalUnderstaffedDays,
            TightDays = totalTightDays,
            CriticalMonths = criticalMonths,
            Months = months,
            CurrentMonthCoverage = currentMonthCoverage
        };

        var criticalNote = criticalMonths.Count > 0
            ? $" Critical month(s): {string.Join(", ", criticalMonths)}."
            : " No understaffed days (demand never exceeds maximum capacity).";
        var message =
            $"Resource monitor {year}{GroupNote(groupId)}: {totalUnderstaffedDays} understaffed day(s) " +
            $"(demand > max capacity), {totalTightDays} tight day(s) (above desired, within max).{criticalNote}";

        return SkillResult.SuccessResult(data, message);
    }

    private static string GroupNote(Guid? groupId)
        => groupId.HasValue ? $" for group {groupId}" : " (all groups)";
}
