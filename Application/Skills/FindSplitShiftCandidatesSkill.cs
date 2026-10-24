// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that lists existing orders (Bestellungen) and already-split shifts in a group, so the
/// assistant can ask the user whether to reuse an existing (already-split) order or open a new one
/// before building a multi-part service. Call this FIRST for any split/24h-rotation request.
/// The count is by group membership only (sealed orders and their shifts linked directly to the group), never by
/// address: every answer also reports how many orders have no group at all and, when the group name is also a city or
/// state, how many orders lie there by customer address (OrderLocationAddressRule, through the visibility-scoped shift
/// list), so "0 in the group" is never passed off as "0 in the place".
/// </summary>
/// <param name="groupName">Name of the group to inspect (e.g. "Biel/Bienne").</param>
/// <param name="searchString">Optional text filter on the order name.</param>
/// <param name="mediator">Runs the visibility-scoped shift list for the address-based counts</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("find_split_shift_candidates")]
public class FindSplitShiftCandidatesSkill : BaseSkillImplementation
{
    private const int GroupSearchLimit = 10;
    private const int CountOnlyPageSize = 1;

    private readonly IShiftReader _shiftReader;
    private readonly IGroupSearchRepository _groupSearchRepository;
    private readonly IMediator _mediator;

    public FindSplitShiftCandidatesSkill(
        IShiftReader shiftReader,
        IGroupSearchRepository groupSearchRepository,
        IMediator mediator)
    {
        _shiftReader = shiftReader;
        _groupSearchRepository = groupSearchRepository;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetParameter<string>(parameters, "groupName");
        var searchString = (GetParameter<string>(parameters, "searchString") ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(groupName))
        {
            return SkillResult.Error("Parameter 'groupName' is required to inspect existing orders of a group.");
        }

        var groupResult = await _groupSearchRepository.SearchAsync(
            searchTerm: groupName,
            limit: GroupSearchLimit,
            cancellationToken: cancellationToken);

        if (groupResult.TotalCount == 0)
        {
            return SkillResult.Error($"No group found matching '{groupName}'.");
        }

        if (groupResult.TotalCount > 1)
        {
            var matches = groupResult.Items.Select(g => new { g.Id, g.Name }).ToList();
            return SkillResult.SuccessResult(
                new { EntityType = "group", Matches = matches, Count = matches.Count },
                $"Multiple groups match '{groupName}'. Ask the user which one before inspecting its orders.");
        }

        var group = groupResult.Items.First();

        var shifts = await _shiftReader.GetOrderFamilyShiftsInGroupAsync(group.Id, searchString, cancellationToken);

        // Group every row by its order: a SealedOrder is keyed by its own Id, the plannable shift
        // and its split parts by their OriginalId. After a cut the plannable shift is converted into
        // a part, so the SealedOrder is the stable header for name/span.
        var orders = shifts
            .GroupBy(s => s.OriginalId ?? s.Id)
            .Select(g =>
            {
                var sealedOrder = g.FirstOrDefault(s => s.Status == ShiftStatus.SealedOrder);
                var orderShift = g.FirstOrDefault(s => s.Status == ShiftStatus.OriginalShift);
                var parts = g.Where(s => s.Status == ShiftStatus.SplitShift)
                    .OrderBy(s => s.StartShift)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        StartTime = s.StartShift.ToString("HH\\:mm"),
                        EndTime = s.EndShift.ToString("HH\\:mm")
                    })
                    .ToList();

                var header = sealedOrder ?? orderShift ?? g.First();
                return new
                {
                    OrderShiftId = orderShift?.Id,
                    SealedOrderId = g.Key,
                    Name = header.Name,
                    Span = $"{header.StartShift:HH\\:mm}-{header.EndShift:HH\\:mm}",
                    FromDate = header.FromDate,
                    AlreadySplit = parts.Count > 0,
                    PartCount = parts.Count,
                    Parts = parts
                };
            })
            .OrderByDescending(o => o.AlreadySplit)
            .ThenBy(o => o.Name)
            .ToList();

        var splitCount = orders.Count(o => o.AlreadySplit);
        var ungrouped = await _shiftReader.CountUngroupedOrdersAsync(cancellationToken);
        var inCity = await CountOrdersAtAsync(city: group.Name, state: null, cancellationToken);
        var inState = await CountOrdersAtAsync(city: null, state: group.Name, cancellationToken);

        var message = (orders.Count == 0
                          ? $"Group '{group.Name}' has no orders linked to it. "
                          : $"Group '{group.Name}' has {orders.Count} order(s) linked to it, {splitCount} already split. ") +
                      ScopeNote(group.Name, orders.Count, ungrouped, inCity, inState) +
                      NextStep(orders.Count, inCity.Total > 0 || inState.Total > 0);

        var scope = new
        {
            CountedBy = "direct group membership of sealed orders and their shifts, not the customer address",
            UngroupedOpenOrders = ungrouped.OpenOrders,
            UngroupedSealedOrders = ungrouped.SealedOrders,
            OrdersByAddressInCity = new { inCity.OpenOrders, inCity.SealedOrders },
            OrdersByAddressInState = new { inState.OpenOrders, inState.SealedOrders }
        };

        return SkillResult.SuccessResult(
            new { Group = new { group.Id, group.Name }, OrderCount = orders.Count, AlreadySplitCount = splitCount, Orders = orders, Scope = scope },
            message);
    }

    private static string ScopeNote(
        string groupName, int linkedCount, OrderStatusCounts ungrouped, OrderStatusCounts inCity, OrderStatusCounts inState)
    {
        var note = "Scope: this counts group membership only (sealed orders and their shifts linked to this group), " +
                   "not where the customers are - never conclude from it that a place has no orders. ";

        if (ungrouped.Total > 0)
        {
            note += $"{ungrouped.OpenOrders} open (unsealed) and {ungrouped.SealedOrders} sealed order(s) belong to no group at all. ";
        }

        if (inCity.Total > 0 || inState.Total > 0)
        {
            note += "By customer address (workplace, else main address): " +
                    $"city '{groupName}' {inCity.OpenOrders} open and {inCity.SealedOrders} sealed order(s), " +
                    $"state '{groupName}' {inState.OpenOrders} open and {inState.SealedOrders} sealed order(s) - the city " +
                    "lies inside the state, so never add the two up. " +
                    $"Tell the user the difference to the {linkedCount} sealed order(s) linked to the group. search_shifts " +
                    "with city or state and status draft_order or sealed_order lists them; assign_orders_to_groups can link " +
                    "open orders to their groups. ";
        }

        return note;
    }

    private static string NextStep(int linkedCount, bool foundByAddress)
    {
        if (linkedCount > 0)
        {
            return "ASK THE USER whether to reuse one of these existing (already-split) orders or open a NEW order; " +
                   "do not create anything before the user decides.";
        }

        return foundByAddress
            ? "ASK THE USER whether one of the orders found by address should be used or a NEW order created; " +
              "do not create anything before the user decides."
            : "Ask the user to confirm creating a NEW order, then create_shift + cut_shift.";
    }

    private async Task<OrderStatusCounts> CountOrdersAtAsync(string? city, string? state, CancellationToken cancellationToken)
    {
        var open = await CountOrdersInViewAsync(city, state, isSealedOrder: false, cancellationToken);
        var sealedOrders = await CountOrdersInViewAsync(city, state, isSealedOrder: true, cancellationToken);
        return new OrderStatusCounts(open, sealedOrders);
    }

    private async Task<int> CountOrdersInViewAsync(
        string? city, string? state, bool isSealedOrder, CancellationToken cancellationToken)
    {
        var filter = new ShiftFilter
        {
            FilterType = ShiftFilterType.Original,
            IsSealedOrder = isSealedOrder,
            ActiveDateRange = true,
            FormerDateRange = true,
            FutureDateRange = true,
            IsTimeRange = true,
            IsSporadic = true,
            NumberOfItemsPerPage = CountOnlyPageSize,
            City = city,
            State = state
        };
        var result = await _mediator.Send(new GetTruncatedListQuery(filter), cancellationToken);
        return Math.Max(result?.MaxItems ?? 0, 0);
    }
}
