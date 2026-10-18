// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IClientPlanningPeriodGuard: refuses a day outside the employee's membership (inclusive bounds, an open end
/// allows everything later) or outside the group period the schedule grid would derive for that employee (earliest
/// ValidFrom and latest ValidUntil of the employee's items in the visible groups). The visible groups are those of the
/// request's selected group, or, without a selection, the ones the grid itself uses; with none (an unrestricted caller
/// without a selection) no group period applies. An employee without a membership row and without group bounds is not
/// blocked, exactly like the grid. The earliest offending day is reported; FindViolationsAsync lists every offending cell
/// without throwing, so a batch planner can reject single placements instead of the whole batch;
/// FindViolationsInGroupsAsync reads the group period of explicitly named groups (the target group of a planner run)
/// instead of the request's group context.
/// </summary>
/// <param name="clientRepository">Loads the employees with their membership</param>
/// <param name="groupItemRepository">Reads the group period bounds of the employees</param>
/// <param name="groupContextResolver">The visible groups of the request's selected group, null without one</param>
/// <param name="groupFilterService">The groups the grid uses when no group is selected</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.ShiftSchedule;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ClientPlanningPeriodGuard : IClientPlanningPeriodGuard
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string OpenEndText = "open";

    private const string OutsideMembershipMessage =
        "{0} cannot be planned on {1}: the day is outside the membership period ({2} to {3}).";

    private const string OutsideGroupPeriodMessage =
        "{0} cannot be planned on {1}: the day is outside the group period of the employee ({2} to {3}).";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IShiftGroupFilterService _groupFilterService;

    public ClientPlanningPeriodGuard(
        IClientRepository clientRepository,
        IGroupItemRepository groupItemRepository,
        ISelectedGroupContextResolver groupContextResolver,
        IShiftGroupFilterService groupFilterService)
    {
        _clientRepository = clientRepository;
        _groupItemRepository = groupItemRepository;
        _groupContextResolver = groupContextResolver;
        _groupFilterService = groupFilterService;
    }

    public async Task EnsureWithinPeriodsAsync(
        IReadOnlyCollection<(Guid ClientId, DateOnly Date)> cells,
        CancellationToken cancellationToken)
    {
        var violations = await FindViolationsAsync(cells, cancellationToken);
        if (violations.Count > 0)
        {
            throw ToRefusal(violations[0]);
        }
    }

    public async Task<IReadOnlyList<PlanningPeriodViolation>> FindViolationsAsync(
        IReadOnlyCollection<(Guid ClientId, DateOnly Date)> cells,
        CancellationToken cancellationToken)
    {
        if (cells.Count == 0)
        {
            return [];
        }

        return await FindViolationsCoreAsync(cells, await ResolveVisibleGroupIdsAsync(), cancellationToken);
    }

    public async Task<IReadOnlyList<PlanningPeriodViolation>> FindViolationsInGroupsAsync(
        IReadOnlyCollection<(Guid ClientId, DateOnly Date)> cells,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken)
    {
        if (cells.Count == 0)
        {
            return [];
        }

        return await FindViolationsCoreAsync(cells, groupIds, cancellationToken);
    }

    private async Task<IReadOnlyList<PlanningPeriodViolation>> FindViolationsCoreAsync(
        IReadOnlyCollection<(Guid ClientId, DateOnly Date)> cells,
        IReadOnlyCollection<Guid> visibleGroupIds,
        CancellationToken cancellationToken)
    {
        var clientIds = cells.Select(cell => cell.ClientId).Distinct().ToList();
        var clients = (await _clientRepository.GetByIdsAsync(clientIds, cancellationToken))
            .GroupBy(client => client.Id)
            .ToDictionary(group => group.Key, group => group.First());

        var groupRanges = visibleGroupIds.Count == 0
            ? new Dictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)>()
            : await _groupItemRepository.GetValidityRangesAsync(clientIds, visibleGroupIds, cancellationToken);

        var violations = new List<PlanningPeriodViolation>();
        foreach (var cell in cells.Distinct().OrderBy(cell => cell.Date))
        {
            if (!clients.TryGetValue(cell.ClientId, out var client))
            {
                continue;
            }

            var violation = DescribeViolation(client, cell.Date, groupRanges);
            if (violation != null)
            {
                violations.Add(violation);
            }
        }

        return violations;
    }

    private static PlanningPeriodViolation? DescribeViolation(
        Client client,
        DateOnly date,
        IReadOnlyDictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)> groupRanges)
    {
        if (client.Membership != null
            && IsOutside(date, DateOnly.FromDateTime(client.Membership.ValidFrom), ToDate(client.Membership.ValidUntil)))
        {
            var message = string.Format(
                OutsideMembershipMessage,
                DisplayName(client),
                date.ToString(DateFormat),
                client.Membership.ValidFrom.ToString(DateFormat),
                Describe(client.Membership.ValidUntil));
            return new PlanningPeriodViolation(
                client.Id,
                date,
                message,
                WorkBookingRefusalCodes.OutsideMembershipPeriod,
                DateOnly.FromDateTime(client.Membership.ValidFrom),
                ToDate(client.Membership.ValidUntil));
        }

        if (groupRanges.TryGetValue(client.Id, out var range)
            && IsOutside(date, ToDate(range.ValidFrom), ToDate(range.ValidUntil)))
        {
            var message = string.Format(
                OutsideGroupPeriodMessage,
                DisplayName(client),
                date.ToString(DateFormat),
                Describe(range.ValidFrom),
                Describe(range.ValidUntil));
            return new PlanningPeriodViolation(
                client.Id,
                date,
                message,
                WorkBookingRefusalCodes.OutsideGroupPeriod,
                ToDate(range.ValidFrom),
                ToDate(range.ValidUntil));
        }

        return null;
    }

    private static WorkBookingRefusedException ToRefusal(PlanningPeriodViolation violation)
        => new(violation.Message, violation.Code, new Dictionary<string, object?>
        {
            [WorkBookingRefusalCodes.ClientIdField] = violation.ClientId,
            [WorkBookingRefusalCodes.DateField] = violation.Date,
            [WorkBookingRefusalCodes.FromField] = violation.From,
            [WorkBookingRefusalCodes.UntilField] = violation.Until,
        });

    private async Task<IReadOnlyCollection<Guid>> ResolveVisibleGroupIdsAsync()
    {
        var selected = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
        return selected ?? await _groupFilterService.GetVisibleGroupIdsAsync(null);
    }

    private static bool IsOutside(DateOnly date, DateOnly? from, DateOnly? until)
        => (from.HasValue && date < from.Value) || (until.HasValue && date > until.Value);

    private static DateOnly? ToDate(DateTime? value)
        => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static string Describe(DateTime? value)
        => value.HasValue ? value.Value.ToString(DateFormat) : OpenEndText;

    private static string DisplayName(Client client) => $"{client.FirstName} {client.Name}".Trim();
}
