// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that kicks off the AutoWizard chain (Wizard 1 Planner + Wizard 2 Harmonizer + Wizard 3 Holistic
/// Harmonizer) for a given group and period. The group is identified by id or by name, the period by ISO
/// dates or by an ISO calendar week that the skill resolves itself (Monday to Sunday). When agentIds /
/// shiftIds are omitted, the skill resolves them from the group via group_item membership and the
/// visible-shift SP. Returns the orchestrator jobId; the chain runs in the background and ends in a
/// proposal scenario. Stage 3 runs the deterministic local search by default; only when it is switched to an AI
/// model and that model is missing or cannot read the plan image does the result message say up front that the
/// chain will stop after the harmonizer stage.
/// </summary>
/// <param name="groupId">Optional group UUID (the "selectedGroup" of the schedule view); takes precedence over groupName.</param>
/// <param name="groupName">Optional group display name, resolved with fuzzy matching inside the caller's group scope.</param>
/// <param name="periodFrom">Period start date (ISO yyyy-MM-dd); not needed when calendarWeek is given.</param>
/// <param name="periodUntil">Period end date (ISO yyyy-MM-dd, inclusive); not needed when calendarWeek is given.</param>
/// <param name="calendarWeek">Optional ISO calendar week (1-53); takes precedence over periodFrom/periodUntil.</param>
/// <param name="year">Optional ISO week-based year of calendarWeek (current year -1..+1, two digits allowed); defaults to the next occurrence of the week.</param>
/// <param name="agentIds">Optional comma-separated client UUIDs; defaults to the clients the schedule shows for the group and its sub-groups in the period.</param>
/// <param name="shiftIds">Optional comma-separated shift UUIDs; defaults to visible shifts via GetShiftSchedule.</param>
/// <param name="analyseToken">Optional source scenario token; null = main scenario.</param>
/// <param name="language">Optional UI language for Wizard 3 when it runs on an AI model, e.g. "de", "en". Falls back to engine default.</param>

using System.Globalization;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("start_autowizard")]
public class StartAutoWizardSkill : BaseSkillImplementation
{
    private const string GroupIdParameter = "groupId";
    private const string GroupNameParameter = "groupName";
    private const string PeriodFromParameter = "periodFrom";
    private const string PeriodUntilParameter = "periodUntil";
    private const string CalendarWeekParameter = "calendarWeek";
    private const string YearParameter = "year";
    private const int FirstCalendarWeek = 1;
    private const int DaysAfterMonday = 6;
    private const int MaxCalendarWeek = 53;
    private const int YearWindow = 1;
    private const int MaxTwoDigitYear = 99;
    private const int TwoDigitYearCentury = 2000;

    private readonly IAutoWizardJobRunner _autoWizardJobRunner;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupPlanningAgentRepository _planningAgentRepository;
    private readonly IShiftScheduleRepository _shiftScheduleRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly ICompanyClock _companyClock;
    private readonly IHolisticHarmonizerReadinessCheck _holisticReadinessCheck;

    public StartAutoWizardSkill(
        IAutoWizardJobRunner autoWizardJobRunner,
        IGroupRepository groupRepository,
        IGroupPlanningAgentRepository planningAgentRepository,
        IShiftScheduleRepository shiftScheduleRepository,
        IGroupScopeGuard groupScopeGuard,
        ICompanyClock companyClock,
        IHolisticHarmonizerReadinessCheck holisticReadinessCheck)
    {
        _autoWizardJobRunner = autoWizardJobRunner;
        _groupRepository = groupRepository;
        _planningAgentRepository = planningAgentRepository;
        _shiftScheduleRepository = shiftScheduleRepository;
        _groupScopeGuard = groupScopeGuard;
        _companyClock = companyClock;
        _holisticReadinessCheck = holisticReadinessCheck;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupIdStr = GetParameter<string>(parameters, GroupIdParameter);
        var groupName = GetParameter<string>(parameters, GroupNameParameter);
        var agentIdsRaw = GetParameter<string>(parameters, "agentIds");
        var shiftIdsRaw = GetParameter<string>(parameters, "shiftIds");
        var analyseTokenStr = GetParameter<string>(parameters, "analyseToken");
        var language = GetParameter<string>(parameters, "language");

        Guid? groupId = null;
        if (!string.IsNullOrWhiteSpace(groupIdStr))
        {
            if (!Guid.TryParse(groupIdStr, out var parsedGroupId))
            {
                return SkillResult.Error($"Invalid groupId format: '{groupIdStr}'. Expected UUID.");
            }

            groupId = parsedGroupId;
        }
        else if (string.IsNullOrWhiteSpace(groupName))
        {
            return SkillResult.Error("Provide the group to plan, either as groupName or as groupId.");
        }

        var (period, periodError) = await ResolvePeriodAsync(parameters, cancellationToken);
        if (periodError != null)
        {
            return SkillResult.Error(periodError);
        }

        var (periodFrom, periodUntil) = period;

        var (group, groupError) = await ResolveGroupAsync(context, groupId, groupName, cancellationToken);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenStr) && Guid.TryParse(analyseTokenStr, out var parsedToken))
        {
            analyseToken = parsedToken;
        }

        var agentIds = await ResolveAgentIdsAsync(agentIdsRaw, group!.Id, periodFrom, periodUntil, cancellationToken);
        if (agentIds.Count == 0)
        {
            return SkillResult.Error(
                $"No agents resolved for group '{group.Name}'. Either pass explicit agentIds or assign clients to the group first.");
        }

        var shiftIds = await ResolveShiftIdsAsync(shiftIdsRaw, group.Id, periodFrom, periodUntil, analyseToken, cancellationToken);
        if (shiftIds.Count == 0)
        {
            return SkillResult.Error(
                $"No shifts visible for group '{group.Name}' in period {periodFrom}..{periodUntil}. " +
                "Either pass explicit shiftIds or assign shifts to the group's hierarchy first.");
        }

        var request = new StartAutoWizardRequest(
            PeriodFrom: periodFrom,
            PeriodUntil: periodUntil,
            AgentIds: agentIds,
            ShiftIds: shiftIds,
            GroupId: group.Id,
            AnalyseToken: analyseToken,
            Language: language,
            ContextDaysBefore: ScenarioConstants.BoundaryDays,
            ContextDaysAfter: ScenarioConstants.BoundaryDays);

        var holisticReadiness = await _holisticReadinessCheck.CheckAsync(cancellationToken);

        Guid jobId;
        try
        {
            jobId = await _autoWizardJobRunner.StartAsync(request, CancellationToken.None);
        }
        catch (AutofillLimitExceededException ex)
        {
            // The guard already knows the measured and permitted figures; repeating the check here would
            // be a second copy that can drift from it.
            return SkillResult.Error(ex.Message);
        }
        catch (AutofillRunConflictException ex)
        {
            return SkillResult.Error(
                $"A {ex.Family} job is already running for this period (jobId {ex.RunningJobId}). "
                + "Join it or cancel it first.");
        }

        var stageNote = holisticReadiness.IsReady
            ? "Stages: planning, harmonizing and holistic harmonization."
            : "Only planning and harmonizing will run: the holistic harmonization (stage 3) will be skipped because "
              + $"{holisticReadiness.Reason} Tell the user this now; do not promise three stages.";

        var result = new
        {
            JobId = jobId,
            GroupId = group.Id,
            GroupName = group.Name,
            PeriodFrom = periodFrom,
            PeriodUntil = periodUntil,
            AgentCount = agentIds.Count,
            ShiftCount = shiftIds.Count,
            SourceAnalyseToken = analyseToken,
            HolisticHarmonizationWillRun = holisticReadiness.IsReady,
            HolisticHarmonizationSkipReason = holisticReadiness.Reason,
            Hint = "The run takes a few minutes. Check the job status with this jobId to learn the outcome and the scenario to accept."
        };

        return SkillResult.SuccessResult(
            result,
            $"AutoWizard job {jobId} started for '{group.Name}' covering {periodFrom:yyyy-MM-dd}..{periodUntil:yyyy-MM-dd} " +
            $"({agentIds.Count} agents, {shiftIds.Count} shifts). It runs in the background and ends in a proposal " +
            $"scenario; the real schedule only changes once the user accepts it. {stageNote}");
    }

    /// <summary>
    /// Resolves the planning period: an ISO calendar week wins (Monday to Sunday of that week); otherwise both
    /// ISO dates are required. A week without a year means its next occurrence - the current week or a later
    /// one of the company's current ISO week-based year, an earlier week number the same week of the next one -
    /// because nobody asks to plan a week that is already over.
    /// </summary>
    private async Task<((DateOnly From, DateOnly Until) Period, string? Error)> ResolvePeriodAsync(
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var calendarWeek = GetParameter<int?>(parameters, CalendarWeekParameter);
        if (calendarWeek.HasValue)
        {
            return await ResolveCalendarWeekAsync(
                calendarWeek.Value, GetParameter<int?>(parameters, YearParameter), cancellationToken);
        }

        var periodFromStr = GetParameter<string>(parameters, PeriodFromParameter);
        var periodUntilStr = GetParameter<string>(parameters, PeriodUntilParameter);
        if (string.IsNullOrWhiteSpace(periodFromStr) || string.IsNullOrWhiteSpace(periodUntilStr))
        {
            return (default, "Provide the period, either as calendarWeek or as periodFrom and periodUntil (ISO yyyy-MM-dd).");
        }

        var periodFrom = GetParameter<DateOnly?>(parameters, PeriodFromParameter);
        if (!periodFrom.HasValue)
        {
            return (default, $"Invalid periodFrom format: '{periodFromStr}'. Expected ISO yyyy-MM-dd.");
        }

        var periodUntil = GetParameter<DateOnly?>(parameters, PeriodUntilParameter);
        if (!periodUntil.HasValue)
        {
            return (default, $"Invalid periodUntil format: '{periodUntilStr}'. Expected ISO yyyy-MM-dd.");
        }

        if (periodFrom.Value > periodUntil.Value)
        {
            return (default, $"periodFrom ({periodFrom.Value}) must be on or before periodUntil ({periodUntil.Value}).");
        }

        return ((periodFrom.Value, periodUntil.Value), null);
    }

    /// <summary>
    /// Turns an ISO calendar week (and an optional week-based year) into Monday..Sunday. The year must lie
    /// within one year of the company's current ISO week-based year; a two-digit year means the 2000s and is
    /// only accepted when that reading falls inside the same window.
    /// </summary>
    private async Task<((DateOnly From, DateOnly Until) Period, string? Error)> ResolveCalendarWeekAsync(
        int calendarWeek, int? requestedYear, CancellationToken cancellationToken)
    {
        if (calendarWeek < FirstCalendarWeek || calendarWeek > MaxCalendarWeek)
        {
            return (default, $"Calendar week {calendarWeek} does not exist; valid weeks are {FirstCalendarWeek}-{MaxCalendarWeek}.");
        }

        var today = (await _companyClock.GetTodayDateAsync(cancellationToken)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var currentIsoYear = ISOWeek.GetYear(today);
        var minYear = currentIsoYear - YearWindow;
        var maxYear = currentIsoYear + YearWindow;

        int year;
        if (requestedYear.HasValue)
        {
            year = requestedYear.Value is >= 0 and <= MaxTwoDigitYear
                ? TwoDigitYearCentury + requestedYear.Value
                : requestedYear.Value;
            if (year < minYear || year > maxYear)
            {
                return (default,
                    $"Year {requestedYear.Value} is not supported for calendar week planning; use a year between {minYear} and {maxYear}.");
            }
        }
        else
        {
            year = calendarWeek < ISOWeek.GetWeekOfYear(today) ? currentIsoYear + 1 : currentIsoYear;
        }

        var weeksInYear = ISOWeek.GetWeeksInYear(year);
        if (calendarWeek > weeksInYear)
        {
            return (default, $"Calendar week {calendarWeek} does not exist in {year}; valid weeks are {FirstCalendarWeek}-{weeksInYear}.");
        }

        var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(year, calendarWeek, DayOfWeek.Monday));
        return ((monday, monday.AddDays(DaysAfterMonday)), null);
    }

    private async Task<(Group? Group, string? Error)> ResolveGroupAsync(
        SkillExecutionContext context, Guid? groupId, string? groupName, CancellationToken cancellationToken)
    {
        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);

        Group? group;
        if (groupId.HasValue)
        {
            group = await _groupRepository.Get(groupId.Value);
            if (group == null)
            {
                return (null, $"Group with ID {groupId} not found.");
            }
        }
        else
        {
            var groups = scope.Filter(await _groupRepository.List());
            var (resolved, resolveError) = GroupResolver.Resolve(groups, groupName);
            if (resolveError != null)
            {
                return (null, resolveError);
            }

            group = resolved!;
        }

        return scope.IsInScope(group)
            ? (group, null)
            : (null, scope.BuildOutOfScopeError(group.Name));
    }

    private async Task<IReadOnlyList<Guid>> ResolveAgentIdsAsync(
        string? agentIdsRaw,
        Guid groupId,
        DateOnly periodFrom,
        DateOnly periodUntil,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(agentIdsRaw))
        {
            return agentIdsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();
        }

        return await _planningAgentRepository.GetAgentIdsAsync(groupId, periodFrom, periodUntil, cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> ResolveShiftIdsAsync(
        string? shiftIdsRaw,
        Guid groupId,
        DateOnly periodFrom,
        DateOnly periodUntil,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(shiftIdsRaw))
        {
            return shiftIdsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();
        }

        var filter = new ShiftScheduleFilter
        {
            StartDate = periodFrom,
            EndDate = periodUntil,
            SelectedGroup = groupId,
            AnalyseToken = analyseToken,
            StartRow = 0,
            RowCount = int.MaxValue
        };

        var (shifts, _) = await _shiftScheduleRepository.GetShiftScheduleAsync(filter, cancellationToken);
        return shifts.Select(s => s.ShiftId).Distinct().ToList();
    }
}
