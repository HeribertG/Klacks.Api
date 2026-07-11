// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sweeps a group's active employee members for one period and reports each member's actual
/// hours, guaranteed (target) hours and the resulting balance — the same numbers get_period_hours
/// shows for a single employee, computed here for a whole group in one call via the shared
/// PeriodHours query. Lets Klacksy answer "who has overtime" / "distribute shifts more fairly"
/// with data instead of asking back. Read-only — no apply parameter.
/// </summary>
/// <param name="groupName">Required. Name (or partial name) of the group to sweep.</param>
/// <param name="startDate">Required. Period start (yyyy-MM-dd).</param>
/// <param name="endDate">Required. Period end (yyyy-MM-dd).</param>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Queries.PeriodHours;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_group_hours_balance")]
public class GetGroupHoursBalanceSkill : BaseSkillImplementation
{
    private const string DateFormat = "yyyy-MM-dd";
    private const int AverageBalanceDecimals = 1;

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;

    public GetGroupHoursBalanceSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IMediator mediator)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, "groupName");
        var startDate = GetParameter<DateOnly?>(parameters, "startDate")
            ?? throw new ArgumentException("Required parameter 'startDate' is missing");
        var endDate = GetParameter<DateOnly?>(parameters, "endDate")
            ?? throw new ArgumentException("Required parameter 'endDate' is missing");

        if (endDate < startDate)
        {
            return SkillResult.Error(
                $"endDate {endDate.ToString(DateFormat)} must not be before startDate {startDate.ToString(DateFormat)}.");
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List());
        var (group, groupError) = GroupResolver.Resolve(groups, groupName);
        if (group == null)
        {
            return SkillResult.Error(groupError!);
        }

        var members = await _mediator.Send(new GetGroupMembersQuery(group.Id), cancellationToken);
        var activeEmployees = members
            .Where(m => m.ClientId.HasValue
                && m.Client != null
                && !m.Client.IsDeleted
                && m.Client.Type == (int)EntityTypeEnum.Employee
                && MembershipOverlapsPeriod(m.ValidFrom, m.ValidUntil, startDate, endDate))
            .ToList();

        if (activeEmployees.Count == 0)
        {
            return SkillResult.SuccessResult(
                new GroupHoursBalanceResult(group.Id, group.Name, startDate, endDate, 0, []),
                $"Group '{group.Name}' has no active employee members between {startDate.ToString(DateFormat)} " +
                $"and {endDate.ToString(DateFormat)} — nothing to balance.");
        }

        var clientIds = activeEmployees.Select(m => m.ClientId!.Value).ToList();
        var hoursByClient = await _mediator.Send(
            new GetPeriodHoursQuery(new PeriodHoursRequest
            {
                ClientIds = clientIds,
                StartDate = startDate,
                EndDate = endDate,
                AnalyseToken = null
            }),
            cancellationToken);

        var balances = activeEmployees
            .Select(m => ToBalance(m, hoursByClient))
            .OrderBy(b => b.Balance)
            .ToList();

        var result = new GroupHoursBalanceResult(
            group.Id, group.Name, startDate, endDate, balances.Count, balances);

        var lowest = balances[0];
        var highest = balances[^1];
        var average = Math.Round(balances.Average(b => b.Balance), AverageBalanceDecimals);

        return SkillResult.SuccessResult(
            result,
            $"Hours balance for group '{group.Name}' from {startDate.ToString(DateFormat)} to " +
            $"{endDate.ToString(DateFormat)} across {balances.Count} active employee(s): average balance " +
            $"{FormatSigned(average)}h. Lowest: {lowest.FirstName} {lowest.LastName} " +
            $"({FormatSigned(lowest.Balance)}h). Highest: {highest.FirstName} {highest.LastName} " +
            $"({FormatSigned(highest.Balance)}h). Members are sorted from lowest to highest balance — " +
            "relay each member's actual vs. target hours and balance to the user.");
    }

    private static GroupMemberHoursBalance ToBalance(
        GroupItemResource member,
        Dictionary<Guid, PeriodHoursResource> hoursByClient)
    {
        var clientId = member.ClientId!.Value;
        var hours = hoursByClient.TryGetValue(clientId, out var resource)
            ? resource
            : new PeriodHoursResource();

        return new GroupMemberHoursBalance(
            clientId,
            member.Client!.FirstName ?? string.Empty,
            member.Client!.Name ?? string.Empty,
            hours.Hours,
            hours.Surcharges,
            hours.GuaranteedHours,
            hours.Hours - hours.GuaranteedHours);
    }

    private static bool MembershipOverlapsPeriod(
        DateTime? validFrom, DateTime? validUntil, DateOnly startDate, DateOnly endDate)
    {
        var startsInTimeOrBefore = validFrom is null || DateOnly.FromDateTime(validFrom.Value) <= endDate;
        var endsInTimeOrAfter = validUntil is null || DateOnly.FromDateTime(validUntil.Value) >= startDate;
        return startsInTimeOrBefore && endsInTimeOrAfter;
    }

    private static string FormatSigned(decimal value) => value >= 0 ? $"+{value}" : value.ToString();
}
