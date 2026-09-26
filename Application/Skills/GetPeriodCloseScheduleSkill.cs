// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports when periods are closed: the close date of a period is its last day plus a lag of days. Without a
/// lag it reports the facts the conversation needs (today, the stored lag if any, the global autonomy level and,
/// per group, whether Klacksy would close that group's periods on its own); with a lag it computes, per staffed
/// group, the close date of the running period and of the previous (last ended) period while that one is still
/// open: not sealed on its last day (the same check as the period detectors) and holding work on the group's own
/// shifts (the scope a group seal acts on, as in PeriodAutoCloseService). An open previous period is reported
/// both while it waits for its close date and once that date has been reached (PreviousPeriodCloseDue), so an
/// already due period is never hidden. Whether a group is closed automatically is not derived from the global level alone but read from
/// IPeriodAutoCloseResolver - the same gate PeriodAutoCloseService obeys (kill switch, the period_auto_close
/// governance rule at Execute, global level and the minimum over ALL admins at FullyAutonomous) - so the skill can
/// never promise a close that the service would not perform. Period boundaries come from the group's
/// PaymentInterval exactly like the period detectors. Read-only: nothing is stored here.
/// </summary>
/// <param name="groupRepository">Lists the groups and which of them have members</param>
/// <param name="weekConfiguration">Resolves the configured week start for weekly period boundaries</param>
/// <param name="settingsReader">Reads the stored lag</param>
/// <param name="governanceResolver">Reads the installation-wide autonomy level</param>
/// <param name="autoCloseResolver">Per group, whether the autonomous close is allowed and what blocks it</param>
/// <param name="companyClock">Resolves today as the company's own local day</param>
/// <param name="sealedDayRepository">Tells whether the previous period's last day is already sealed</param>
/// <param name="activityProbe">Tells whether the previous period holds work a group seal would act on</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_period_close_schedule")]
public class GetPeriodCloseScheduleSkill : BaseSkillImplementation
{
    private const string IsoDateFormat = "yyyy-MM-dd";
    private const string ContextMode = "Context";
    private const string ComputedMode = "Computed";
    private const int MaxReportedGroups = 25;
    private const string Formula = "close date = period end + lag days (never before the period has ended)";
    private const string AutoCloseConditions =
        "Klacksy closes a group's period on its own only when ALL of these hold: a lag is stored; the governance "
        + "rule period_auto_close is set to Execute (its default is Hint); the global proactive autonomy level is "
        + "FullyAutonomous; EVERY admin has chosen the level FullyAutonomous; the kill switch is off. "
        + "AutoCloseBlockedBy names the brake per group (None = closes automatically). Groups where it is not "
        + "allowed get no automatic close and no message about it - only the usual reminders.";

    private readonly IGroupRepository _groupRepository;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly ISettingsReader _settingsReader;
    private readonly IProactiveGovernanceResolver _governanceResolver;
    private readonly IPeriodAutoCloseResolver _autoCloseResolver;
    private readonly ICompanyClock _companyClock;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IScheduleActivityProbe _activityProbe;

    public GetPeriodCloseScheduleSkill(
        IGroupRepository groupRepository,
        IWeekConfiguration weekConfiguration,
        ISettingsReader settingsReader,
        IProactiveGovernanceResolver governanceResolver,
        IPeriodAutoCloseResolver autoCloseResolver,
        ICompanyClock companyClock,
        ISealedDayRepository sealedDayRepository,
        IScheduleActivityProbe activityProbe)
    {
        _groupRepository = groupRepository;
        _weekConfiguration = weekConfiguration;
        _settingsReader = settingsReader;
        _governanceResolver = governanceResolver;
        _autoCloseResolver = autoCloseResolver;
        _companyClock = companyClock;
        _sealedDayRepository = sealedDayRepository;
        _activityProbe = activityProbe;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var storedLag = await PeriodCloseLagReader.ReadAsync(_settingsReader);
        var lag = GetParameter<int?>(parameters, PeriodCloseParameters.LagDays);
        var level = await _governanceResolver.GetGlobalAutonomyLevelAsync(cancellationToken);

        if (lag is not null && !PeriodCloseDateCalculator.IsValidLag(lag.Value))
        {
            return SkillResult.Error(
                $"{PeriodCloseParameters.LagDays} must be a whole number of days between "
                + $"{PeriodCloseDateCalculator.MinLagDays} and {PeriodCloseDateCalculator.MaxLagDays}.");
        }

        var (groups, totalGroups) = await ListReportableGroupsAsync(cancellationToken);
        var gates = await ResolveGatesAsync(groups, cancellationToken);
        var anyGroupAllowed = gates.Values.Any(decision => decision.CanClose);

        return lag is null
            ? BuildContextResult(today, storedLag, level, groups, totalGroups, gates, anyGroupAllowed)
            : await BuildComputedResultAsync(
                today, lag.Value, storedLag, level, groups, totalGroups, gates, anyGroupAllowed, cancellationToken);
    }

    private static SkillResult BuildContextResult(
        DateOnly today,
        int? storedLag,
        AutonomyLevel level,
        IReadOnlyList<Group> groups,
        int totalGroups,
        IReadOnlyDictionary<Guid, PeriodAutoCloseDecision> gates,
        bool anyGroupAllowed)
    {
        var storedText = storedLag.HasValue
            ? $"stored lag {storedLag.Value} day(s) after the period end"
            : "no lag stored, so periods are never closed automatically";
        var autoCloseText = anyGroupAllowed && storedLag.HasValue
            ? "automatic closing is active for the groups with AutoCloseBlockedBy None"
            : "automatic closing is currently active for no group";

        return SkillResult.SuccessResult(
            new
            {
                Mode = ContextMode,
                Today = FormatDate(today),
                StoredLagDays = storedLag,
                LagStored = storedLag.HasValue,
                MinLagDays = PeriodCloseDateCalculator.MinLagDays,
                MaxLagDays = PeriodCloseDateCalculator.MaxLagDays,
                GlobalAutonomyLevel = level.ToString(),
                AutonomyAllowsAutoClose = anyGroupAllowed,
                AutoCloseConditions,
                Groups = groups.Select(group => new GroupGateRow(
                    group.Name,
                    gates[group.Id].CanClose,
                    gates[group.Id].BlockedBy.ToString())).ToList(),
                OmittedGroups = totalGroups - groups.Count,
                Formula
            },
            $"Period close context: today is {FormatDate(today)}, {storedText}; global autonomy level {level}; "
            + $"{autoCloseText}. {AutoCloseConditions}");
    }

    private async Task<SkillResult> BuildComputedResultAsync(
        DateOnly today,
        int lag,
        int? storedLag,
        AutonomyLevel level,
        IReadOnlyList<Group> groups,
        int totalGroups,
        IReadOnlyDictionary<Guid, PeriodAutoCloseDecision> gates,
        bool anyGroupAllowed,
        CancellationToken cancellationToken)
    {
        var weekStart = await _weekConfiguration.GetWeekStartAsync(today, cancellationToken);
        var nextWeekStart = weekStart.AddDays(NextPeriodBoundaries.WeeklyPeriodDays);
        var rows = new List<GroupCloseRow>(groups.Count);
        foreach (var group in groups)
        {
            rows.Add(await BuildRowAsync(group, today, lag, nextWeekStart, gates[group.Id], cancellationToken));
        }

        rows = rows
            .OrderBy(row => row.CurrentPeriodCloseDate, StringComparer.Ordinal)
            .ThenBy(row => row.GroupName, StringComparer.Ordinal)
            .ToList();

        return SkillResult.SuccessResult(
            new
            {
                Mode = ComputedMode,
                Today = FormatDate(today),
                LagDays = lag,
                StoredLagDays = storedLag,
                LagStored = storedLag.HasValue,
                GlobalAutonomyLevel = level.ToString(),
                AutonomyAllowsAutoClose = anyGroupAllowed,
                AutoCloseConditions,
                Formula,
                Groups = rows,
                OmittedGroups = totalGroups - rows.Count
            },
            $"With a lag of {lag} day(s) after the period end, {totalGroups} group(s) with a derivable "
            + "period have a close date. PreviousPeriod* is only present when the last ended period is still open "
            + "(not sealed, holding work); PreviousPeriodCloseDue true means its close date has already been "
            + "reached, so it is due for closing now. Individual groups have no derivable cycle and are not listed. "
            + "LagStored says whether a lag is stored at all (without one periods are never closed automatically). "
            + $"{AutoCloseConditions} Nothing is stored by this skill.");
    }

    /// <summary>
    /// The staffed groups with a derivable cycle, capped at MaxReportedGroups: only those are reported, so the
    /// autonomy gate is resolved for no more groups than the answer can show.
    /// </summary>
    private async Task<(IReadOnlyList<Group> Reported, int Total)> ListReportableGroupsAsync(
        CancellationToken cancellationToken)
    {
        var groups = await _groupRepository.List();
        var staffing = GroupStaffingLookup.Build(
            groups, await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));
        var eligible = groups
            .Where(group => NextPeriodBoundaries.HasDerivableCycle(group.PaymentInterval) && staffing.IsStaffed(group.Id))
            .OrderBy(group => group.Name, StringComparer.Ordinal)
            .ToList();

        return (eligible.Take(MaxReportedGroups).ToList(), eligible.Count);
    }

    private async Task<IReadOnlyDictionary<Guid, PeriodAutoCloseDecision>> ResolveGatesAsync(
        IReadOnlyList<Group> groups, CancellationToken cancellationToken)
    {
        var gates = new Dictionary<Guid, PeriodAutoCloseDecision>();
        foreach (var group in groups)
        {
            gates[group.Id] = await _autoCloseResolver.ResolveAsync(group.Id, cancellationToken);
        }

        return gates;
    }

    private async Task<GroupCloseRow> BuildRowAsync(
        Group group,
        DateOnly today,
        int lagDays,
        DateOnly nextWeekStart,
        PeriodAutoCloseDecision gate,
        CancellationToken cancellationToken)
    {
        var currentEnd = NextPeriodBoundaries.ComputeStart(group, today, nextWeekStart).AddDays(-1);
        var currentClose = PeriodCloseDateCalculator.CloseDateFor(group.PaymentInterval, currentEnd, lagDays)!.Value;
        var currentStart = PeriodBoundaries.StartFor(group.PaymentInterval, currentEnd);
        var previousEnd = currentStart.AddDays(-1);
        var previousStart = PeriodBoundaries.StartFor(group.PaymentInterval, previousEnd);
        var previousClose = PeriodCloseDateCalculator.CloseDateFor(group.PaymentInterval, previousEnd, lagDays)!.Value;
        var previousOpen = await IsOpenWithWorkAsync(group, previousStart, previousEnd, cancellationToken);

        return new GroupCloseRow(
            group.Name,
            FormatDate(currentEnd),
            FormatDate(currentClose),
            currentClose.DayNumber - today.DayNumber,
            previousOpen ? FormatDate(previousEnd) : null,
            previousOpen ? FormatDate(previousClose) : null,
            previousOpen ? previousClose.DayNumber - today.DayNumber : null,
            previousOpen ? previousClose <= today : null,
            gate.CanClose,
            gate.BlockedBy.ToString());
    }

    /// <summary>
    /// True when the period is still open and would be affected by closing it: its last day is not sealed for
    /// the group (a group or global seal) and work on the group's own shifts falls into it.
    /// </summary>
    private async Task<bool> IsOpenWithWorkAsync(
        Group group, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken)
    {
        var seals = await _sealedDayRepository.GetRangeAsync(periodEnd, periodEnd, group.Id, cancellationToken);
        if (seals.Count > 0)
        {
            return false;
        }

        return await _activityProbe.HasDirectWorkInRangeAsync(group, periodStart, periodEnd, cancellationToken);
    }

    private static string FormatDate(DateOnly date) => date.ToString(IsoDateFormat, CultureInfo.InvariantCulture);

    private sealed record GroupGateRow(string GroupName, bool AutoCloseAllowed, string AutoCloseBlockedBy);

    private sealed record GroupCloseRow(
        string GroupName,
        string CurrentPeriodEnd,
        string CurrentPeriodCloseDate,
        int CurrentPeriodDaysUntilClose,
        string? PreviousPeriodEnd,
        string? PreviousPeriodCloseDate,
        int? PreviousPeriodDaysUntilClose,
        bool? PreviousPeriodCloseDue,
        bool AutoCloseAllowed,
        string AutoCloseBlockedBy);
}
