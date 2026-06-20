// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that lists existing orders (Bestellungen) and already-split shifts in a group, so the
/// assistant can ask the user whether to reuse an existing (already-split) order or open a new one
/// before building a multi-part service. Call this FIRST for any split/24h-rotation request.
/// </summary>
/// <param name="groupName">Name of the group to inspect (e.g. "Biel/Bienne").</param>
/// <param name="searchString">Optional text filter on the order name.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("find_split_shift_candidates")]
public class FindSplitShiftCandidatesSkill : BaseSkillImplementation
{
    private const int GroupSearchLimit = 10;

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupSearchRepository _groupSearchRepository;

    public FindSplitShiftCandidatesSkill(
        IShiftRepository shiftRepository,
        IGroupSearchRepository groupSearchRepository)
    {
        _shiftRepository = shiftRepository;
        _groupSearchRepository = groupSearchRepository;
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

        var query = _shiftRepository.GetQuery()
            .Where(s => s.GroupItems.Any(gi => gi.GroupId == group.Id && !gi.IsDeleted))
            .Where(s => s.Status == ShiftStatus.SealedOrder
                        || s.Status == ShiftStatus.OriginalShift
                        || s.Status == ShiftStatus.SplitShift);

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{searchString}%"));
        }

        var shifts = await query
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Abbreviation,
                s.Status,
                s.StartShift,
                s.EndShift,
                s.FromDate,
                s.UntilDate,
                s.OriginalId
            })
            .ToListAsync(cancellationToken);

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

        var message = orders.Count == 0
            ? $"Group '{group.Name}' has no existing orders. Ask the user to confirm creating a NEW order, then create_shift + cut_shift."
            : $"Group '{group.Name}' has {orders.Count} order(s), {splitCount} already split. ASK THE USER whether to reuse one of these " +
              "existing (already-split) orders or open a NEW order; do not create anything before the user decides.";

        return SkillResult.SuccessResult(
            new { Group = new { group.Id, group.Name }, OrderCount = orders.Count, AlreadySplitCount = splitCount, Orders = orders },
            message);
    }
}
