// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only diagnosis of why a day cannot be planned from the planning periods alone: the employment (membership) period,
/// the periods of the group memberships and the day seals, evaluated exactly like the schedule grid does (a day outside the
/// employment is shown dark, a day outside a group membership is shown blue and locked in that group's schedule). Takes the
/// employee (id, or name plus optional client number), a day or a short range (at most 31 days) and optionally one group,
/// whose subtree limits the memberships looked at. A hidden employee is answered exactly like an unknown one, memberships in
/// groups outside the caller's group scope are only counted, never named. The answer is a structured status per day plus
/// English sentences with the step that lifts each finding; it writes nothing. Without an employee it summarizes a whole
/// group (by name or id) for the same days, over the employees the schedule lists for it: how many are dark, how many are
/// blue, the most common and the earliest start of the late memberships, and the sealed days, read in a few batched queries.
/// The blue days of that summary come from the memberships in exactly the selected group, like the grid, which matches the
/// selected group id exactly and not its subgroups; an employee who only belongs to a subgroup is listed but never blue.
/// </summary>
/// <param name="clientId">Optional. UUID of the employee; otherwise firstName/lastName</param>
/// <param name="firstName">Optional. First name of the employee</param>
/// <param name="lastName">Optional. Last name of the employee</param>
/// <param name="idNumber">Optional. Client number that tells apart people with the same name</param>
/// <param name="date">Required. The day to check, or the first day of a range</param>
/// <param name="untilDate">Optional. Last day of a range of at most 31 days; empty checks the single day</param>
/// <param name="groupName">Optional. Name of the group whose schedule is meant; empty looks at all groups the caller may see. Required (or groupId) without an employee</param>
/// <param name="groupId">Optional. UUID of the group, as an alternative to groupName; the selectedGroupId of the page context when the user means the group on screen</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillNames.DiagnosePlanningPeriod)]
public class DiagnosePlanningPeriodSkill : BaseSkillImplementation
{
    public const int MaxRangeDays = 31;

    private const string ClientIdParameter = "clientId";
    private const string FirstNameParameter = "firstName";
    private const string LastNameParameter = "lastName";
    private const string DateParameter = "date";
    private const string UntilDateParameter = "untilDate";
    private const string GroupNameParameter = "groupName";
    private const string GroupIdParameter = "groupId";

    private const string MissingSubjectMessage =
        "Please provide the employee (clientId, or firstName and lastName) or the group (groupName, or groupId; for the " +
        "cells on screen the selectedGroupId of the page context) to check.";
    private const string GroupIdNotFoundMessage = "No group with this id exists in your group scope.";
    private const string MissingDateMessage = "Required parameter 'date' is missing. Ask the user for the day to check.";
    private const string RangeOrderMessage = "The untilDate lies before the date. Ask the user for the correct range.";
    private const string RangeTooLongMessage =
        "The range is longer than {0} days. Ask the user for a shorter range, or check single days.";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly ICompanyClock _companyClock;
    private readonly IGroupPlanningAgentRepository _planningAgentRepository;
    private readonly IGroupItemRepository _groupItemRepository;

    public DiagnosePlanningPeriodSkill(
        IClientSearchRepository clientSearchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        ISealedDayRepository sealedDayRepository,
        ICompanyClock companyClock,
        IGroupPlanningAgentRepository planningAgentRepository,
        IGroupItemRepository groupItemRepository)
    {
        _clientSearchRepository = clientSearchRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _sealedDayRepository = sealedDayRepository;
        _companyClock = companyClock;
        _planningAgentRepository = planningAgentRepository;
        _groupItemRepository = groupItemRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (range, rangeError) = ReadRange(parameters, today, context.UserLanguage);
        if (rangeError is not null)
        {
            return SkillResult.Error(rangeError);
        }

        var clientIdRaw = GetParameter<string>(parameters, ClientIdParameter);
        var firstName = GetParameter<string>(parameters, FirstNameParameter);
        var lastName = GetParameter<string>(parameters, LastNameParameter);
        if (string.IsNullOrWhiteSpace(clientIdRaw) && string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return await SummarizeGroupAsync(context, parameters, range, cancellationToken);
        }

        var (client, clientError) = await ClientResolver.ResolveByIdOrNameAsync(
            _clientSearchRepository, _clientRepository, _clientVisibilityGuard, clientIdRaw, firstName, lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken);
        if (client == null || !await _clientVisibilityGuard.IsVisibleAsync(client.Id, cancellationToken))
        {
            return SkillResult.Error(clientError ?? ClientResolver.NotFoundMessage(firstName, lastName));
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var (view, groupError) = await ReadMembershipsAsync(
            client, GetParameter<string>(parameters, GroupNameParameter), GetParameter<string>(parameters, GroupIdParameter),
            scope, cancellationToken);
        if (view is null)
        {
            return SkillResult.Error(groupError!);
        }

        var (sealedDays, closedDays) = await ReadSealsAsync(client.Id, range, cancellationToken);
        var diagnosis = PlanningPeriodDiagnosisBuilder.Build(
            $"{client.FirstName} {client.Name}".Trim(),
            client.Membership is null ? null : ToWindow(client.Membership),
            view.Periods, view.HiddenCount, view.RequestedName, view.NotMember, range.From, range.Until, sealedDays,
            closedDays);

        return SkillResult.SuccessResult(PlanningPeriodDiagnosisData.From(diagnosis), PlanningPeriodDiagnosisMessage.Compose(diagnosis));
    }

    private (DateRange Range, string? Error) ReadRange(Dictionary<string, object> parameters, DateTime today, string? language)
    {
        var rawDate = GetParameter<string>(parameters, DateParameter);
        if (string.IsNullOrWhiteSpace(rawDate))
        {
            return (default, MissingDateMessage);
        }

        var (date, invalidDate) = SkillDateParser.ParseOptionalUtcDate(rawDate, today, language);

        if (invalidDate || date is null)
        {
            return (default, SkillDateParser.InvalidDateMessageFor(DateParameter, rawDate));
        }

        var rawUntil = GetParameter<string>(parameters, UntilDateParameter);
        var (until, invalidUntil) = SkillDateParser.ParseOptionalUtcDate(rawUntil, today, language);
        if (invalidUntil)
        {
            return (default, SkillDateParser.InvalidDateMessageFor(UntilDateParameter, rawUntil!));
        }

        var from = DateOnly.FromDateTime(date.Value);
        var last = until.HasValue ? DateOnly.FromDateTime(until.Value) : from;
        if (last < from)
        {
            return (default, RangeOrderMessage);
        }

        var checkedRange = new DateRange(from, last);
        return checkedRange.DayCount > MaxRangeDays
            ? (default, string.Format(CultureInfo.InvariantCulture, RangeTooLongMessage, MaxRangeDays))
            : (checkedRange, null);
    }

    private async Task<SkillResult> SummarizeGroupAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        DateRange range,
        CancellationToken cancellationToken)
    {
        var groupName = GetParameter<string>(parameters, GroupNameParameter);
        var groupIdRaw = GetParameter<string>(parameters, GroupIdParameter);
        if (string.IsNullOrWhiteSpace(groupName) && string.IsNullOrWhiteSpace(groupIdRaw))
        {
            return SkillResult.Error(MissingSubjectMessage);
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List());
        var (group, groupError) = ResolveGroup(groups, groupName, groupIdRaw);
        if (group is null)
        {
            return SkillResult.Error(groupError!);
        }

        var employments = await _planningAgentRepository.GetAgentEmploymentsAsync(
            group.Id, range.From, range.Until, cancellationToken);
        var members = await ReadGroupMembersAsync(employments, group, cancellationToken);
        var sealedClientDays = members.Count == 0
            ? new HashSet<(Guid ClientId, DateOnly Date)>()
            : await _sealedDayRepository.GetLockedClientDaysAsync(
                members.Select(member => member.ClientId).ToList(), range.From, range.Until, cancellationToken);

        var summary = PlanningGroupSummaryBuilder.Build(group.Name, range.From, range.Until, members, sealedClientDays);
        return SkillResult.SuccessResult(PlanningGroupSummaryData.From(summary), PlanningGroupSummaryMessage.Compose(summary));
    }

    private async Task<List<PlanningGroupMember>> ReadGroupMembersAsync(
        IReadOnlyList<PlanningAgentEmployment> employments,
        Group group,
        CancellationToken cancellationToken)
    {
        if (employments.Count == 0)
        {
            return [];
        }

        var ranges = await _groupItemRepository.GetValidityRangesAsync(
            employments.Select(employment => employment.ClientId).ToList(), [group.Id], cancellationToken);

        return employments
            .Select(employment => ranges.TryGetValue(employment.ClientId, out var range)
                ? new PlanningGroupMember(
                    employment.ClientId, employment.Employment, ToDate(range.ValidFrom), ToDate(range.ValidUntil), true)
                : new PlanningGroupMember(employment.ClientId, employment.Employment, null, null, false))
            .ToList();
    }

    private static (Group? Group, string? Error) ResolveGroup(IReadOnlyList<Group> groups, string? groupName, string? groupIdRaw)
    {
        if (!string.IsNullOrWhiteSpace(groupIdRaw))
        {
            var byId = Guid.TryParse(groupIdRaw, out var groupId) ? groups.FirstOrDefault(group => group.Id == groupId) : null;
            return byId is null ? (null, GroupIdNotFoundMessage) : (byId, null);
        }

        return GroupResolver.Resolve(groups, groupName);
    }

    private static DateOnly? ToDate(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private async Task<(MembershipView? View, string? Error)> ReadMembershipsAsync(
        Client client, string? groupName, string? groupIdRaw, GroupScopeAccess scope, CancellationToken cancellationToken)
    {
        var items = client.GroupItems
            .Where(item => !item.IsDeleted && item.AnalyseToken == null && item.ShiftId == null && item.Group is { IsDeleted: false })
            .ToList();
        var visible = items.Where(item => scope.IsInScope(item.Group!)).ToList();
        var hidden = items.Count - visible.Count;

        string? requestedName = null;
        if (!string.IsNullOrWhiteSpace(groupName) || !string.IsNullOrWhiteSpace(groupIdRaw))
        {
            var groups = scope.Filter(await _groupRepository.List());
            var (group, error) = ResolveGroup(groups, groupName, groupIdRaw);
            if (group == null)
            {
                return (null, error);
            }

            requestedName = group.Name;
            visible = visible.Where(item => GroupSubtree.Contains(group, item.Group!)).ToList();
            if (visible.Count == 0)
            {
                return (new MembershipView([], hidden, requestedName, NotMember: true), null);
            }
        }

        var periods = visible.Select(ToPeriod).OrderBy(period => period.GroupName, StringComparer.Ordinal).ToList();
        return (new MembershipView(periods, hidden, requestedName, NotMember: false), null);
    }

    private async Task<(HashSet<DateOnly> Sealed, HashSet<DateOnly> Closed)> ReadSealsAsync(
        Guid clientId, DateRange range, CancellationToken cancellationToken)
    {
        var pairs = Enumerable.Range(0, range.DayCount)
            .Select(offset => (Date: range.From.AddDays(offset), ClientId: clientId))
            .ToList();
        var sealedPairs = await _sealedDayRepository.GetLockedPairsAsync(pairs, cancellationToken);
        var closedPairs = await _sealedDayRepository.GetLockedPairsAsync(pairs, WorkLockLevel.Closed, cancellationToken);
        return (sealedPairs.Select(pair => pair.Date).ToHashSet(), closedPairs.Select(pair => pair.Date).ToHashSet());
    }

    private static MembershipWindow ToWindow(Membership membership) => new(
        DateOnly.FromDateTime(membership.ValidFrom),
        membership.ValidUntil.HasValue ? DateOnly.FromDateTime(membership.ValidUntil.Value) : null);

    private static PlanningGroupMembershipPeriod ToPeriod(GroupItem item) => new(
        item.GroupId,
        item.Group!.Name,
        item.ValidFrom.HasValue ? DateOnly.FromDateTime(item.ValidFrom.Value) : null,
        item.ValidUntil.HasValue ? DateOnly.FromDateTime(item.ValidUntil.Value) : null);

    private readonly record struct DateRange(DateOnly From, DateOnly Until)
    {
        public int DayCount => Until.DayNumber - From.DayNumber + 1;
    }

    private sealed record MembershipView(
        IReadOnlyList<PlanningGroupMembershipPeriod> Periods, int HiddenCount, string? RequestedName, bool NotMember);
}
