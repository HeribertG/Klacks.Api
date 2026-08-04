// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Judges whether a planned absence request (holiday, training, other placeholder wishes) still
/// leaves enough staffing reserve, or whether granting it runs the team into a resource gap.
/// Demand is the scheduled shifts, capacity is the desired daily readiness minus the absences
/// already planned and the one being requested. A window fails when utilization exceeds the
/// configured ceiling, which is what keeps a buffer for unplanned sickness. Evaluated per day,
/// per rolling three days, per work week and per calendar week. The counterpart that searches for
/// a fitting period instead of judging a given one is find_absence_capacity_windows.
/// </summary>
/// <param name="fromDate">First day of the requested absence, yyyy-MM-dd</param>
/// <param name="untilDate">Last day of the requested absence, yyyy-MM-dd</param>
/// <param name="absenceTypeId">Optional absence type whose DefaultValue is the daily weight of the request</param>
/// <param name="dailyValue">Optional explicit daily weight, used when no absence type is given</param>
/// <param name="groupId">Optional group to restrict the capacity view to</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using System.Globalization;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("check_absence_capacity_reserve")]
public class CheckAbsenceCapacityReserveSkill : AbsenceCapacitySkillBase
{
    private const int MaxReportedWindows = 12;

    public CheckAbsenceCapacityReserveSkill(
        IMediator mediator,
        IAbsenceRepository absenceRepository,
        ISettingsRepository settingsRepository)
        : base(mediator, absenceRepository, settingsRepository)
    {
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate");

        if (fromDate is null || untilDate is null)
        {
            return SkillResult.Error("Both fromDate and untilDate are required (yyyy-MM-dd).");
        }

        var from = fromDate.Value;
        var until = untilDate.Value;
        if (until < from)
        {
            (from, until) = (until, from);
        }

        if (until.DayNumber - from.DayNumber + 1 > MaxRangeDays)
        {
            return SkillResult.Error($"Requested range is longer than {MaxRangeDays} days.");
        }

        var (groupId, groupError) = ParseGroupId(parameters);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        var dailyValueResult = await ResolveDailyValueAsync(parameters);
        if (dailyValueResult.Error != null)
        {
            return SkillResult.Error(dailyValueResult.Error);
        }

        var maxUtilization = await ReadMaxUtilizationAsync();

        var days = await LoadCapacityDaysAsync(from, until, groupId, cancellationToken);
        if (days.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { From = from, Until = until, GroupId = groupId, Evaluated = 0 },
                "No resource-monitor data available for this period, so capacity cannot be judged.");
        }

        var findings = AbsenceCapacityCalculator.Evaluate(days, from, until, dailyValueResult.Value);
        var critical = AbsenceCapacityCalculator.CriticalOnly(findings, maxUtilization);

        var worstPerKind = findings
            .Where(f => f.NoCapacityLeft || f.Utilization.HasValue)
            .GroupBy(f => f.Kind)
            .Select(g => g
                .OrderByDescending(f => f.NoCapacityLeft)
                .ThenByDescending(f => f.Utilization ?? double.MaxValue)
                .First())
            .OrderBy(f => f.Kind)
            .Select(Describe)
            .ToList();

        var reported = critical
            .OrderByDescending(f => f.NoCapacityLeft)
            .ThenByDescending(f => f.Utilization ?? double.MaxValue)
            .Take(MaxReportedWindows)
            .Select(Describe)
            .ToList();

        var data = new
        {
            From = from,
            Until = until,
            GroupId = groupId,
            RequestedDailyValue = dailyValueResult.Value,
            MaxUtilizationPercent = ToPercent(maxUtilization),
            ReserveIsSufficient = critical.Count == 0,
            CriticalWindowCount = critical.Count,
            EvaluatedWindowCount = findings.Count,
            CriticalWindows = reported,
            CriticalWindowsTruncated = critical.Count > reported.Count,
            WorstPerWindowKind = worstPerKind
        };

        var ceiling = ToPercent(maxUtilization).ToString(CultureInfo.InvariantCulture);
        var message = critical.Count == 0
            ? $"Reserve is sufficient: across {findings.Count} window(s) between {from} and {until} " +
              $"utilization stays at or below the {ceiling}% ceiling."
            : $"Resource gap: {critical.Count} of {findings.Count} window(s) between {from} and {until} " +
              $"exceed the {ceiling}% ceiling. Report the listed windows, do not claim the absence is safe, " +
              "and offer find_absence_capacity_windows to look for a period that fits.";

        return SkillResult.SuccessResult(data, message);
    }

    private static object Describe(CapacityWindowFinding finding) => new
    {
        Window = finding.Kind.ToString(),
        From = finding.From,
        Until = finding.Until,
        Demand = Math.Round(finding.Demand, 2),
        Available = Math.Round(finding.Available, 2),
        UtilizationPercent = finding.Utilization.HasValue
            ? ToPercent(finding.Utilization.Value)
            : (double?)null,
        NoCapacityLeft = finding.NoCapacityLeft
    };
}
