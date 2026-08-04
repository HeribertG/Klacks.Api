// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Searches a date range for periods where an absence of the wanted length still fits the staffing
/// reserve, and reports them best-first. Slides the wanted period across the range and judges every
/// placement with the same rule check_absence_capacity_reserve applies to a single given period, so
/// a suggestion made here can never be rejected by a direct check of the same dates. Use this when
/// the user knows how long they want off but not when; use check_absence_capacity_reserve when the
/// dates are already fixed.
/// </summary>
/// <param name="durationDays">Length of the wanted absence in calendar days</param>
/// <param name="searchFrom">Earliest day the absence may start, yyyy-MM-dd</param>
/// <param name="searchUntil">Latest day the absence may end, yyyy-MM-dd</param>
/// <param name="absenceTypeId">Optional absence type whose DefaultValue is the daily weight of the request</param>
/// <param name="dailyValue">Optional explicit daily weight, used when no absence type is given</param>
/// <param name="groupId">Optional group to restrict the capacity view to</param>
/// <param name="maxSuggestions">Optional cap on how many periods are returned</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using System.Globalization;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("find_absence_capacity_windows")]
public class FindAbsenceCapacityWindowsSkill : AbsenceCapacitySkillBase
{
    private const int DefaultMaxSuggestions = 5;
    private const int SuggestionHardCap = 20;
    private const int MaxReportedAlternatives = 3;

    public FindAbsenceCapacityWindowsSkill(
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
        var durationDays = GetParameter<int?>(parameters, "durationDays");
        if (durationDays is null or < 1)
        {
            return SkillResult.Error("durationDays is required and must be at least 1.");
        }

        var searchFromParam = GetParameter<DateOnly?>(parameters, "searchFrom");
        var searchUntilParam = GetParameter<DateOnly?>(parameters, "searchUntil");
        if (searchFromParam is null || searchUntilParam is null)
        {
            return SkillResult.Error("Both searchFrom and searchUntil are required (yyyy-MM-dd).");
        }

        var searchFrom = searchFromParam.Value;
        var searchUntil = searchUntilParam.Value;
        if (searchUntil < searchFrom)
        {
            (searchFrom, searchUntil) = (searchUntil, searchFrom);
        }

        var rangeDays = searchUntil.DayNumber - searchFrom.DayNumber + 1;
        if (rangeDays > MaxRangeDays)
        {
            return SkillResult.Error($"Search range is longer than {MaxRangeDays} days.");
        }

        if (durationDays.Value > rangeDays)
        {
            return SkillResult.Error(
                $"durationDays ({durationDays}) does not fit into the search range of {rangeDays} day(s).");
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

        var maxSuggestions = Math.Clamp(
            GetParameter<int?>(parameters, "maxSuggestions") ?? DefaultMaxSuggestions, 1, SuggestionHardCap);

        var maxUtilization = await ReadMaxUtilizationAsync();

        var days = await LoadCapacityDaysAsync(searchFrom, searchUntil, groupId, cancellationToken);
        if (days.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { SearchFrom = searchFrom, SearchUntil = searchUntil, GroupId = groupId, Evaluated = 0 },
                "No resource-monitor data available for this range, so no period can be suggested.");
        }

        var candidates = AbsenceCapacityCalculator.FindFittingPeriods(
            days, searchFrom, searchUntil, durationDays.Value, dailyValueResult.Value, maxUtilization);

        var fitting = candidates
            .Where(c => c.Fits)
            .OrderBy(c => c.PeakUtilization ?? double.MaxValue)
            .ThenBy(c => c.From)
            .Take(maxSuggestions)
            .Select(Describe)
            .ToList();

        var closest = candidates
            .Where(c => !c.Fits)
            .OrderBy(c => c.BlockingWindowCount)
            .ThenBy(c => c.PeakUtilization ?? double.MaxValue)
            .ThenBy(c => c.From)
            .Take(MaxReportedAlternatives)
            .Select(Describe)
            .ToList();

        var data = new
        {
            SearchFrom = searchFrom,
            SearchUntil = searchUntil,
            GroupId = groupId,
            DurationDays = durationDays.Value,
            RequestedDailyValue = dailyValueResult.Value,
            MaxUtilizationPercent = ToPercent(maxUtilization),
            EvaluatedPeriodCount = candidates.Count,
            FittingPeriodCount = candidates.Count(c => c.Fits),
            Suggestions = fitting,
            ClosestMisses = fitting.Count == 0 ? closest : []
        };

        var ceiling = ToPercent(maxUtilization).ToString(CultureInfo.InvariantCulture);
        var message = fitting.Count > 0
            ? $"Found {candidates.Count(c => c.Fits)} period(s) of {durationDays} day(s) between {searchFrom} " +
              $"and {searchUntil} that stay within the {ceiling}% ceiling; the best {fitting.Count} are listed, " +
              "lowest peak utilization first."
            : $"No period of {durationDays} day(s) between {searchFrom} and {searchUntil} stays within the " +
              $"{ceiling}% ceiling. Report the closest misses as the near-options they are, do not present " +
              "them as safe.";

        return SkillResult.SuccessResult(data, message);
    }

    private static object Describe(CapacityWindowCandidate candidate) => new
    {
        From = candidate.From,
        Until = candidate.Until,
        Fits = candidate.Fits,
        PeakUtilizationPercent = candidate.PeakUtilization.HasValue
            ? ToPercent(candidate.PeakUtilization.Value)
            : (double?)null,
        BlockingWindowCount = candidate.BlockingWindowCount
    };
}
