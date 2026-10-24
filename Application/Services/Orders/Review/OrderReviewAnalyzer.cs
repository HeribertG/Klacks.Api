// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns the open orders of a review into draft items with the server's proposals, then bundles them by pattern
/// (OrderReviewBundler). Per order: the fields that still block sealing (other than the group, which the review can
/// fill), the group that will staff it (its own, else the OrderGroupPlanner proposal), and for a full-day order the
/// proposed start 07:00 (unless it already starts then) and the standard 3x8 split from that start, judged against
/// the scheduling rules of the executing group's members. 2x12 is never proposed. Qualifications are suggested by
/// name (QualificationNameHeuristic); when there is no suggestion the qualification question is settled. Expenses
/// always stay unasked: the dialog asks for them per order or bundle. Orders that cannot follow a pattern - blocking
/// fields, no group in sight, a split the rules allow only for some or no members - are outliers reviewed one by one.
/// Pure; reads nothing.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewAnalyzer
{
    private const string BlockingFieldsReasonFormat = "order fields missing: {0}";
    private const string NoGroupReasonFormat = "no group could be proposed: {0}";
    private const string NoGroupProposal = "no group proposal";
    private const string SplitForSomeReasonFormat = "the 3x8 split is allowed only for {0} of {1} group members";
    private const string SplitForNoneReason = "the 3x8 split is not allowed for any group member";
    private const string SignatureSeparator = "|";
    private const string ListSeparator = ",";

    public static List<OrderReviewDraftItem> Analyze(OrderReviewAnalysisInput input)
    {
        var analyzed = input.Orders
            .Select(order => AnalyzeOrder(order, input))
            .ToList();

        OrderReviewBundler.AssignKeys(analyzed);

        return analyzed.Select(entry => entry.Item).ToList();
    }

    private static (OrderReviewDraftItem Item, string Signature) AnalyzeOrder(Shift order, OrderReviewAnalysisInput input)
    {
        var item = new OrderReviewDraftItem
        {
            OrderId = order.Id,
            OrderName = order.Name,
            CustomerName = order.Client != null ? OrderGroupPlanner.DisplayName(order.Client) : string.Empty,
            FromDate = order.FromDate,
            UntilDate = order.UntilDate,
            IsFullDay = FullDayOrderRule.IsFullDay(order),
            CurrentStart = order.StartShift,
            CurrentEnd = order.EndShift,
            BlockingFields = BlockingFields(order, input),
        };

        var executingGroupId = ApplyGroup(item, order, input);
        ApplyStartAndSplit(item, executingGroupId, input);
        ApplyQualifications(item, order, input);
        item.OutlierReason = OutlierReason(item);

        return (item, Signature(item, order, executingGroupId));
    }

    private static List<string> BlockingFields(Shift order, OrderReviewAnalysisInput input) =>
        input.MissingRequirementsByOrderId.TryGetValue(order.Id, out var missing)
            ? missing.Where(field => field != OrderSealingService.MissingGroupRequirement).ToList()
            : [];

    private static Guid? ApplyGroup(OrderReviewDraftItem item, Shift order, OrderReviewAnalysisInput input)
    {
        var existing = order.GroupItems.FirstOrDefault(link => !link.IsDeleted && link.AnalyseToken == null);
        if (existing != null)
        {
            item.HasGroup = true;
            item.CurrentGroupId = existing.GroupId;
            item.CurrentGroupName = input.GroupNamesById.GetValueOrDefault(existing.GroupId);
            return existing.GroupId;
        }

        if (input.ProposedGroupsByOrderId.TryGetValue(order.Id, out var proposal))
        {
            item.ProposedGroupId = proposal.GroupId;
            item.ProposedGroupName = proposal.GroupName;
            item.GroupSource = proposal.MatchReason;
            return proposal.GroupId;
        }

        item.GroupSource = input.UnassignableReasonsByOrderId.GetValueOrDefault(order.Id) ?? NoGroupProposal;
        return null;
    }

    private static void ApplyStartAndSplit(OrderReviewDraftItem item, Guid? executingGroupId, OrderReviewAnalysisInput input)
    {
        if (!item.IsFullDay)
        {
            item.ChosenStart = item.CurrentStart;
            item.ChosenParts = [];
            return;
        }

        if (item.CurrentStart == OrderReviewDefaults.ProposedFullDayStart)
        {
            item.ChosenStart = item.CurrentStart;
        }
        else
        {
            item.ProposedStart = OrderReviewDefaults.ProposedFullDayStart;
        }

        item.ProposedParts = FullDayOrderRule.EqualParts(
            item.ProposedStart ?? item.CurrentStart, OrderReviewDefaults.StandardPartCount);

        var policies = executingGroupId.HasValue
            ? input.MemberPoliciesByGroupId.GetValueOrDefault(executingGroupId.Value) ?? []
            : (IReadOnlyList<SchedulingPolicy>)[];
        var verdict = OrderSplitRuleEvaluator.Evaluate(FullDayOrderRule.PartHours(item.ProposedParts[0]), policies);
        item.SplitVerdict = verdict.Kind;
        item.SplitAllowedMembers = verdict.AllowedMembers;
        item.SplitMemberCount = verdict.MemberCount;
    }

    private static void ApplyQualifications(OrderReviewDraftItem item, Shift order, OrderReviewAnalysisInput input)
    {
        var alreadyRequired = order.RequiredQualifications
            .Where(requirement => !requirement.IsDeleted)
            .Select(requirement => requirement.QualificationId)
            .ToHashSet();
        var suggestions = QualificationNameHeuristic.Suggest(
            [order.Name, order.Description, item.CurrentGroupName, item.ProposedGroupName],
            input.Qualifications,
            alreadyRequired);

        item.ProposedQualificationIds = suggestions.Select(qualification => qualification.Id).ToList();
        item.ProposedQualificationNames = suggestions
            .Select(qualification => qualification.Name.GetValueOrFirstAvailable(input.Language))
            .ToList();
        if (suggestions.Count == 0)
        {
            item.ChosenQualificationIds = [];
        }
    }

    private static string? OutlierReason(OrderReviewDraftItem item)
    {
        if (item.BlockingFields.Count > 0)
        {
            return string.Format(BlockingFieldsReasonFormat, string.Join(ListSeparator, item.BlockingFields));
        }

        if (!item.HasGroup && item.ProposedGroupId == null)
        {
            return string.Format(NoGroupReasonFormat, item.GroupSource);
        }

        return item.SplitVerdict switch
        {
            OrderSplitVerdictKind.AllowedForSome =>
                string.Format(SplitForSomeReasonFormat, item.SplitAllowedMembers, item.SplitMemberCount),
            OrderSplitVerdictKind.NotAllowed => SplitForNoneReason,
            _ => null,
        };
    }

    private static string Signature(OrderReviewDraftItem item, Shift order, Guid? executingGroupId) =>
        string.Join(
            SignatureSeparator,
            item.IsFullDay,
            item.CurrentStart.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
            item.CurrentEnd.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
            WeekdayMask(order),
            executingGroupId,
            item.GroupSource,
            string.Join(ListSeparator, item.ProposedQualificationIds.OrderBy(id => id)),
            item.SplitVerdict);

    private static string WeekdayMask(Shift order) =>
        string.Concat(
            new[]
            {
                order.IsMonday, order.IsTuesday, order.IsWednesday, order.IsThursday, order.IsFriday,
                order.IsSaturday, order.IsSunday, order.IsHoliday,
            }.Select(flag => flag ? '1' : '0'));
}
