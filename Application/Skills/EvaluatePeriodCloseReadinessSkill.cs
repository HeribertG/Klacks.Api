// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises whether a person should close one period of one group now. Without dates it picks the period the automatic
/// close would pick (PeriodCloseGates); when that one is sealed it moves on to the last ended period, which may still
/// be waiting for its close date. "Wait" and "nothing to seal" follow the policy of the automatic close, and the answer
/// says that a person can still close by hand. The error count comes from GetPeriodIssuesQuery, the source the automatic close uses:
/// the validation loader alone would miss warnings a Block rule escalates to errors, which the close handler counts.
/// That path carries the loader's documented side effect (the compensatory-rest state is reconciled and saved); apart
/// from it the skill changes nothing and seals nothing. A group is required: a period cycle, the close date and the
/// sealed state exist per group, and SealedDayRepository without a group returns the day locks of every group.
/// The group is resolved with the Strict policy of close_period, so the advice is about the group a close would hit.
/// GetSealingSummaryAsync counts container children, as the seal acts on them too, hence only days are reported here.
/// </summary>
/// <param name="startDate">Optional. Period start in ISO yyyy-MM-dd (inclusive); pass together with endDate.</param>
/// <param name="endDate">Optional. Period end in ISO yyyy-MM-dd (inclusive); pass together with startDate.</param>
/// <param name="groupId">Optional. UUID of the group; takes precedence over groupName. One of the two is required.</param>
/// <param name="groupName">Optional. Display name of the group.</param>

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Application.Services.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluatePeriodCloseReadinessSkill : BaseSkillImplementation
{
    public const string SkillName = "evaluate_period_close_readiness";

    private const string StartDateParameter = "startDate";
    private const string EndDateParameter = "endDate";
    private const string GroupIdParameter = "groupId";
    private const string GroupNameParameter = "groupName";
    private const string GroupRequiredMessage =
        "Name the group (groupId or groupName): readiness is evaluated per group, because period, close date and seal state belong to a group.";
    private const string DatesTogetherMessage = "Pass startDate and endDate together, or neither to use the group's current period.";

    private readonly IMediator _mediator;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly ISettingsReader _settingsReader;
    private readonly ICompanyClock _companyClock;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IScheduleActivityProbe _activityProbe;
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly IWorkRepository _workRepository;

    public EvaluatePeriodCloseReadinessSkill(
        IMediator mediator,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IWeekConfiguration weekConfiguration,
        ISettingsReader settingsReader,
        ICompanyClock companyClock,
        ISealedDayRepository sealedDayRepository,
        IScheduleActivityProbe activityProbe,
        IPeriodAuditLogRepository auditLogRepository,
        IWorkRepository workRepository)
    {
        _mediator = mediator;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _weekConfiguration = weekConfiguration;
        _settingsReader = settingsReader;
        _companyClock = companyClock;
        _sealedDayRepository = sealedDayRepository;
        _activityProbe = activityProbe;
        _auditLogRepository = auditLogRepository;
        _workRepository = workRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var startDate = GetParameter<DateOnly?>(parameters, StartDateParameter);
        var endDate = GetParameter<DateOnly?>(parameters, EndDateParameter);
        if (startDate.HasValue != endDate.HasValue)
        {
            return SkillResult.Error(DatesTogetherMessage);
        }

        if (startDate > endDate)
        {
            return SkillResult.Error($"startDate ({startDate}) must be on or before endDate ({endDate}).");
        }

        var (groupId, _, groupError) = await PeriodClosingGroupResolver.ResolveAsync(
            GetParameter<string>(parameters, GroupIdParameter),
            GetParameter<string>(parameters, GroupNameParameter),
            _groupRepository, _groupScopeGuard, context, cancellationToken);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        var group = groupId.HasValue ? await _groupRepository.Get(groupId.Value) : null;
        if (group == null)
        {
            return SkillResult.Error(GroupRequiredMessage);
        }

        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var lagDays = await PeriodCloseLagReader.ReadAsync(_settingsReader);
        var candidate = startDate.HasValue
            ? new PeriodCloseCandidate(group, startDate.Value, endDate!.Value)
            : await FindCurrentCandidateAsync(group, today, lagDays, cancellationToken);
        if (candidate == null)
        {
            return SkillResult.Error(
                $"No ended period can be derived for group {group.Name}; pass startDate and endDate.");
        }

        var evaluation = PeriodCloseReadinessAdvisor.Evaluate(
            await GatherFactsAsync(candidate, today, lagDays, cancellationToken));

        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }

    /// <summary>
    /// The candidate of the automatic close (the latest due period). When that one is already sealed or there is
    /// none, the last ended period takes over, so a period still waiting for its close date is answered with "wait".
    /// </summary>
    private async Task<PeriodCloseCandidate?> FindCurrentCandidateAsync(
        Group group, DateOnly today, int? lagDays, CancellationToken cancellationToken)
    {
        var groups = await _groupRepository.List();
        var staffing = GroupStaffingLookup.Build(
            groups, await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));

        var dueBound = PeriodCloseDateCalculator.DuePeriodEndBound(today, lagDays ?? 0);
        var due = PeriodCloseGates.FindCandidate(
            group, staffing, dueBound, await _weekConfiguration.GetWeekStartAsync(dueBound, cancellationToken));
        if (due != null && !await PeriodCloseGates.IsLastDaySealedAsync(_sealedDayRepository, due, cancellationToken))
        {
            return due;
        }

        var ended = PeriodCloseGates.FindCandidate(
            group, staffing, today, await _weekConfiguration.GetWeekStartAsync(today, cancellationToken));

        return ended ?? due;
    }

    private async Task<PeriodCloseReadinessFacts> GatherFactsAsync(
        PeriodCloseCandidate candidate, DateOnly today, int? lagDays, CancellationToken cancellationToken)
    {
        var (group, periodStart, periodEnd) = candidate;
        var stop = await PeriodCloseGates.FirstStopAsync(
            _sealedDayRepository, _activityProbe, _auditLogRepository, candidate, cancellationToken);

        var sealedDays = await _sealedDayRepository.GetRangeAsync(periodStart, periodEnd, group.Id, cancellationToken);
        var sealedDates = sealedDays.Select(day => day.Date).ToHashSet();
        var summary = await _workRepository.GetSealingSummaryAsync(periodStart, periodEnd, group.Id, cancellationToken);
        var unsealedDays = summary
            .Where(day => day.Total > 0 && !sealedDates.Contains(day.Date))
            .Select(day => day.Date)
            .OrderBy(day => day)
            .ToList();

        var hasWorkOnChildGroups = stop == PeriodCloseStop.NoDirectWork
            && await _activityProbe.HasWorkInRangeAsync(group, periodStart, periodEnd, cancellationToken);

        var isCheckable = stop is PeriodCloseStop.None or PeriodCloseStop.ReopenedBefore;
        var issues = isCheckable
            ? await _mediator.Send(new GetPeriodIssuesQuery(periodStart, periodEnd, group.Id), cancellationToken)
            : [];

        return new PeriodCloseReadinessFacts(
            group.Id,
            group.Name,
            periodStart,
            periodEnd,
            today,
            lagDays,
            stop,
            hasWorkOnChildGroups,
            PeriodCloseGates.CountErrors(issues),
            issues.Count(issue => issue.Severity == ScheduleValidationType.Warning),
            sealedDates.Count,
            unsealedDays);
    }
}
