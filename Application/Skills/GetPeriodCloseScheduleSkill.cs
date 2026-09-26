// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports when periods are closed: the close date of a period is its last day plus a lag of days. Without a
/// lag it reports the facts the conversation needs (today, the stored lag if any, the global autonomy level and
/// whether Klacksy would close each group's periods on its own); with a lag it computes, per staffed group, the
/// close date of the running period and of the previous (last ended) period while that one is still open: not
/// sealed on its last day (the same check as the period detectors) and holding work on the group's own shifts (the
/// scope a group seal acts on, as in PeriodAutoCloseService). An open previous period is reported both while it
/// waits for its close date and once that date has been reached (PreviousPeriodCloseDue), so an already due period
/// is never hidden. Whether a group is closed automatically is not derived from the global level alone but read
/// from IPeriodAutoCloseResolver - the same gate PeriodAutoCloseService obeys - so the skill can never promise a
/// close that the service would not perform. Period boundaries come from the group's PaymentInterval exactly like
/// the period detectors. Read-only: nothing is stored here.
///
/// Size: the result is fed back to the model through ToolResultFormatter, which cuts every result at
/// LLMLoopConstants.DefaultMaxToolResultChars. Live 2026-09-26 fifteen groups produced about 9200 characters - the
/// conditions text twice and a reason sentence on every row - so the last groups were cut off and the model said
/// their names were not available. Hence: the conditions are stated once (in the message), each distinct reason
/// sentence once in AutoCloseReasons with the names of the groups it applies to, and a row carries only the dates
/// and AutoCloseAllowed (null fields omitted). Rows are ordered so that a period due now comes first, then by close
/// date and name, and are added only while they fit MaxGroupRowsChars; every group left out (beyond
/// MaxReportedGroups or the character budget) is counted in OmittedGroups, which the recipe note tells the model to
/// mention. The reason sentences come from PeriodAutoCloseReasonTexts and name the brakes by the labels of the
/// settings pages, never by internal values (live 2026-09-26 an answer quoted "BlockedBy: MaxAction").
/// Which groups are listed: GroupRepository.List() (every group that is not soft-deleted, no visibility or scope
/// filter), minus Individual groups and groups that neither hold clients or shifts themselves nor have a
/// descendant that does (GroupStaffingLookup) - so a parent group appears as soon as one of its descendants gets
/// a member or shift.
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
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
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
    private const int MaxGroupRowsChars = 4500;
    private const int RowSeparatorChars = 1;
    private const string Formula = "close date = period end + lag days (never before the period has ended)";
    private const string AutoCloseConditions =
        "Klacksy closes a group's period on its own only when ALL of these hold: a lag is stored; in Klacksy Scope "
        + "of Action the rule \"Automatic period close\" is set to \"Carry out\" (its default only reports) and the "
        + "global autonomy level is \"Carry out, including multi-step\"; EVERY administrator has chosen \"Fully "
        + "autonomous\" under Klacksy Autonomy; the master off switch is off. AutoCloseReasons gives each reason "
        + "once with the groups it applies to; relay it in the user's language and never quote field names or "
        + "internal values.";

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
            ? "automatic closing is active for the groups whose reason says so"
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
                OmittedGroups = totalGroups - groups.Count,
                AutoCloseReasons = GroupByReason(groups, gates)
            },
            $"Period close context: today is {FormatDate(today)}, {storedText}; {autoCloseText}. {AutoCloseConditions} "
            + $"Formula: {Formula}.");
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
        var rows = new List<(Group Group, GroupCloseRow Row)>(groups.Count);
        foreach (var group in groups)
        {
            rows.Add((group, await BuildRowAsync(group, today, lag, nextWeekStart, gates[group.Id], cancellationToken)));
        }

        var reported = FitToBudget(rows
            .OrderByDescending(entry => entry.Row.PreviousPeriodCloseDue == true)
            .ThenBy(entry => entry.Row.CurrentPeriodCloseDate, StringComparer.Ordinal)
            .ThenBy(entry => entry.Row.GroupName, StringComparer.Ordinal)
            .ToList());
        var reportedGroups = reported.Select(entry => entry.Group).ToList();

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
                OmittedGroups = totalGroups - reported.Count,
                AutoCloseReasons = GroupByReason(reportedGroups, gates),
                Groups = reported.Select(entry => entry.Row).ToList()
            },
            $"With a lag of {lag} day(s) after the period end: one row per group in Groups. PreviousPeriod* is only "
            + "present when the last ended period is still open (not sealed, holding work); PreviousPeriodCloseDue true "
            + "means that close date has been reached, so the period is due now. OmittedGroups counts groups not listed. "
            + $"LagStored says whether a lag is stored at all. {AutoCloseConditions} Nothing is stored by this skill.");
    }

    /// <summary>
    /// The rows in their order, as long as their serialized size fits MaxGroupRowsChars. The first row is always
    /// kept, so a single very long group name cannot empty the answer.
    /// </summary>
    private static List<(Group Group, GroupCloseRow Row)> FitToBudget(IReadOnlyList<(Group Group, GroupCloseRow Row)> ordered)
    {
        var kept = new List<(Group Group, GroupCloseRow Row)>(ordered.Count);
        var used = 0;
        foreach (var entry in ordered)
        {
            var size = JsonSerializer.Serialize(entry.Row).Length + RowSeparatorChars;
            if (kept.Count > 0 && used + size > MaxGroupRowsChars)
            {
                break;
            }

            kept.Add(entry);
            used += size;
        }

        return kept;
    }

    private static List<ReasonGroups> GroupByReason(
        IReadOnlyList<Group> groups, IReadOnlyDictionary<Guid, PeriodAutoCloseDecision> gates) =>
        groups
            .GroupBy(group => gates[group.Id].BlockedBy)
            .OrderBy(reason => reason.Key)
            .Select(reason => new ReasonGroups(
                PeriodAutoCloseReasonTexts.For(reason.Key),
                reason.Key == PeriodAutoCloseBlockedBy.None,
                reason.Select(group => group.Name).OrderBy(name => name, StringComparer.Ordinal).ToList()))
            .ToList();

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
            previousOpen ? FormatDate(previousEnd) : null,
            previousOpen ? FormatDate(previousClose) : null,
            previousOpen ? previousClose <= today : null,
            gate.CanClose);
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

    private sealed record ReasonGroups(string Reason, bool ClosesAutomatically, IReadOnlyList<string> Groups);

    private sealed record GroupCloseRow(
        string GroupName,
        string CurrentPeriodEnd,
        string CurrentPeriodCloseDate,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PreviousPeriodEnd,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PreviousPeriodCloseDate,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? PreviousPeriodCloseDue,
        bool AutoCloseAllowed);
}
