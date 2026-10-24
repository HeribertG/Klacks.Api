// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies a set_order_review_decision call to the targeted draft items that are still to do. Order of the answers:
/// skip; start (full-day orders only; an equal split follows the new start, a custom split must be decided again);
/// group (only for orders without one and only with full access; GroupResolver under the strict policy); split
/// (OrderReviewSplitSpec, refused when the members' rules allow it for nobody, stored with a hint when only for some);
/// accept the remaining proposals (never a split the rules forbid); qualifications ("none" or known names); expenses
/// (given, declined for these orders, or declined for every order still open). Afterwards the rule verdict of every
/// touched item is recomputed for its current split and executing group.
/// </summary>
/// <param name="memberPolicyReader">Reads the scheduling policies of an executing group's members.</param>
/// <param name="qualificationRepository">Lists the qualifications a name is matched against.</param>
/// <param name="companyClock">The company's today, the date the members' policies are read for.</param>

using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Orders.Review;

public sealed class OrderReviewDecisionApplier : IOrderReviewDecisionApplier
{
    private const string QualificationsNone = "none";
    private const char NameSeparator = ',';

    private const string SkippedFormat = "left {0} order(s) out of this review";
    private const string StartFormat = "start {0} for {1} order(s)";
    private const string StartNotFullDayFormat = "'{0}' is not a full-day order; its start stays {1}";
    private const string SplitAgainFormat = "'{0}' had a custom split - decide the split again for the new start";
    private const string GroupNeedsFullAccess =
        "Only an administrator with unrestricted group scope gives orders a group in the review.";
    private const string GroupAlreadyFormat = "'{0}' already belongs to group '{1}'; the review does not move orders between groups";
    private const string GroupFormat = "group '{0}' for {1} order(s)";
    private const string SplitFormat = "split {0} for {1} order(s)";
    private const string SplitForbiddenFormat = "'{0}': split {1} refused - {2}";
    private const string SplitHintFormat = "'{0}': split {1} - {2}";
    private const string AcceptedFormat = "took the proposals for {0} order(s)";
    private const string SplitProposalForbiddenFormat = "'{0}': the proposed split is not allowed by the rules - decide the split explicitly";
    private const string QualificationsFormat = "qualifications {0} for {1} order(s)";
    private const string QualificationsNoneText = "none";
    private const string UnknownQualificationsFormat = "unknown qualification(s): {0}. Known: {1}";
    private const string ExpensesFormat = "{0} expense(s) for {1} order(s)";
    private const string DeclinedOnceFormat = "no expenses for {0} order(s)";
    private const string DeclinedFinalFormat = "no expenses for {0} order(s) - not asked again in this review";
    private const string ListSeparator = ", ";

    private readonly IGroupMemberPolicyReader _memberPolicyReader;
    private readonly IQualificationRepository _qualificationRepository;
    private readonly ICompanyClock _companyClock;

    public OrderReviewDecisionApplier(
        IGroupMemberPolicyReader memberPolicyReader,
        IQualificationRepository qualificationRepository,
        ICompanyClock companyClock)
    {
        _memberPolicyReader = memberPolicyReader;
        _qualificationRepository = qualificationRepository;
        _companyClock = companyClock;
    }

    public async Task<OrderReviewDecisionOutcome> ApplyAsync(
        OrderReviewDraft draft,
        IReadOnlyList<OrderReviewDraftItem> targets,
        OrderReviewDecision decision,
        OrderReviewScope scope,
        string? language,
        CancellationToken cancellationToken = default)
    {
        var outcome = new OrderReviewDecisionOutcome();
        var pending = targets.Where(OrderReviewReadiness.IsPending).ToList();
        if (decision.Skip)
        {
            pending.ForEach(item => item.ExecutionStatus = OrderReviewExecutionStatus.Skipped);
            outcome.Applied.Add(string.Format(SkippedFormat, pending.Count));
            return outcome;
        }

        var policies = new PolicyCache(_memberPolicyReader, DateOnly.FromDateTime(await _companyClock.GetTodayAsync(cancellationToken)));

        if (decision.StartTime.HasValue)
        {
            ApplyStart(pending, decision.StartTime.Value, outcome);
        }

        if (decision.GroupName != null)
        {
            ApplyGroup(pending, decision.GroupName, scope, outcome);
        }

        if (decision.Split != null)
        {
            await ApplySplitAsync(pending, decision, language, policies, outcome, cancellationToken);
        }

        if (decision.AcceptProposals)
        {
            AcceptProposals(pending, scope, outcome);
        }

        if (decision.Qualifications != null)
        {
            await ApplyQualificationsAsync(pending, decision.Qualifications, language, outcome, cancellationToken);
        }

        ApplyExpenses(draft, pending, decision, outcome);
        await RefreshVerdictsAsync(pending, policies, cancellationToken);
        return outcome;
    }

    private static void ApplyStart(List<OrderReviewDraftItem> items, TimeOnly start, OrderReviewDecisionOutcome outcome)
    {
        var changed = 0;
        foreach (var item in items)
        {
            if (!item.IsFullDay)
            {
                outcome.Rejected.Add(string.Format(StartNotFullDayFormat, item.OrderName, OrderReviewPresenter.Time(item.CurrentStart)));
                continue;
            }

            var previousStart = item.ChosenStart ?? item.ProposedStart ?? item.CurrentStart;
            item.ChosenStart = start;
            item.ProposedParts = FullDayOrderRule.EqualParts(start, OrderReviewDefaults.StandardPartCount);
            if (item.ChosenParts is { Count: > 0 } chosen)
            {
                if (OrderReviewSplitSpec.IsEqualSplitFrom(chosen, previousStart))
                {
                    item.ChosenParts = FullDayOrderRule.EqualParts(start, chosen.Count);
                }
                else
                {
                    item.ChosenParts = null;
                    outcome.Hints.Add(string.Format(SplitAgainFormat, item.OrderName));
                }
            }

            changed++;
        }

        if (changed > 0)
        {
            outcome.Applied.Add(string.Format(StartFormat, OrderReviewPresenter.Time(start), changed));
        }
    }

    private static void ApplyGroup(
        List<OrderReviewDraftItem> items, string groupName, OrderReviewScope scope, OrderReviewDecisionOutcome outcome)
    {
        if (!scope.HasFullAccess)
        {
            outcome.Rejected.Add(GroupNeedsFullAccess);
            return;
        }

        var (group, error) = GroupResolver.Resolve(scope.ReachableGroups, groupName);
        if (group == null)
        {
            outcome.Rejected.Add(error!);
            return;
        }

        var assigned = 0;
        foreach (var item in items)
        {
            if (item.HasGroup)
            {
                outcome.Rejected.Add(string.Format(GroupAlreadyFormat, item.OrderName, item.CurrentGroupName));
                continue;
            }

            item.ChosenGroupId = group.Id;
            item.ChosenGroupName = group.Name;
            assigned++;
        }

        if (assigned > 0)
        {
            outcome.Applied.Add(string.Format(GroupFormat, group.Name, assigned));
        }
    }

    private static async Task ApplySplitAsync(
        List<OrderReviewDraftItem> items,
        OrderReviewDecision decision,
        string? language,
        PolicyCache policies,
        OrderReviewDecisionOutcome outcome,
        CancellationToken cancellationToken)
    {
        var stored = 0;
        foreach (var item in items)
        {
            var (parts, error) = OrderReviewSplitSpec.Resolve(item, decision.Split!, decision.PartNames, language);
            if (parts == null)
            {
                outcome.Rejected.Add(error!);
                continue;
            }

            if (parts.Count > 0)
            {
                var verdict = await policies.EvaluateAsync(item, parts, cancellationToken);
                if (verdict.Kind == OrderSplitVerdictKind.NotAllowed)
                {
                    outcome.Rejected.Add(string.Format(SplitForbiddenFormat, item.OrderName, decision.Split, OrderReviewPresenter.Verdict(item)));
                    continue;
                }

                if (verdict.Kind != OrderSplitVerdictKind.Allowed)
                {
                    outcome.Hints.Add(string.Format(SplitHintFormat, item.OrderName, decision.Split, OrderReviewPresenter.Verdict(item)));
                }
            }

            item.ChosenParts = parts;
            stored++;
        }

        if (stored > 0)
        {
            outcome.Applied.Add(string.Format(SplitFormat, decision.Split, stored));
        }
    }

    private static void AcceptProposals(List<OrderReviewDraftItem> items, OrderReviewScope scope, OrderReviewDecisionOutcome outcome)
    {
        foreach (var item in items)
        {
            item.ChosenStart ??= item.ProposedStart;
            if (item.ChosenParts == null && item.ProposedParts.Count > 0)
            {
                if (item.SplitVerdict == OrderSplitVerdictKind.NotAllowed)
                {
                    outcome.Hints.Add(string.Format(SplitProposalForbiddenFormat, item.OrderName));
                }
                else
                {
                    item.ChosenParts = item.ProposedParts.Select(CopyPart).ToList();
                }
            }

            if (!item.HasGroup && item.ChosenGroupId == null && item.ProposedGroupId.HasValue && scope.HasFullAccess)
            {
                item.ChosenGroupId = item.ProposedGroupId;
                item.ChosenGroupName = item.ProposedGroupName;
            }

            if (item.ChosenQualificationIds == null)
            {
                item.ChosenQualificationIds = item.ProposedQualificationIds.ToList();
                item.ChosenQualificationNames = item.ProposedQualificationNames.ToList();
            }
        }

        outcome.Applied.Add(string.Format(AcceptedFormat, items.Count));
    }

    private async Task ApplyQualificationsAsync(
        List<OrderReviewDraftItem> items,
        string answer,
        string? language,
        OrderReviewDecisionOutcome outcome,
        CancellationToken cancellationToken)
    {
        var chosen = new List<Qualification>();
        if (!string.Equals(answer, QualificationsNone, StringComparison.OrdinalIgnoreCase))
        {
            var known = await _qualificationRepository.GetAllAsync(cancellationToken);
            var names = answer.Split(NameSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var unknown = names.Where(name => FindByName(known, name) == null).ToList();
            if (unknown.Count > 0)
            {
                outcome.Rejected.Add(string.Format(
                    UnknownQualificationsFormat,
                    string.Join(ListSeparator, unknown),
                    string.Join(ListSeparator, known.Select(q => q.Name.GetValueOrFirstAvailable(language)))));
                return;
            }

            chosen = names.Select(name => FindByName(known, name)!).DistinctBy(q => q.Id).ToList();
        }

        foreach (var item in items)
        {
            item.ChosenQualificationIds = chosen.Select(q => q.Id).ToList();
            item.ChosenQualificationNames = chosen.Select(q => q.Name.GetValueOrFirstAvailable(language)).ToList();
        }

        var text = chosen.Count == 0
            ? QualificationsNoneText
            : string.Join(ListSeparator, chosen.Select(q => q.Name.GetValueOrFirstAvailable(language)));
        outcome.Applied.Add(string.Format(QualificationsFormat, text, items.Count));
    }

    private static void ApplyExpenses(
        OrderReviewDraft draft, List<OrderReviewDraftItem> items, OrderReviewDecision decision, OrderReviewDecisionOutcome outcome)
    {
        if (decision.Expenses is { Count: > 0 } expenses)
        {
            foreach (var item in items)
            {
                item.Expenses = expenses.Select(CopyExpense).ToList();
                item.ExpenseStatus = OrderReviewExpenseStatus.Provided;
            }

            outcome.Applied.Add(string.Format(ExpensesFormat, expenses.Count, items.Count));
            return;
        }

        if (decision.ExpenseDecline == OrderReviewExpenseStatus.DeclinedOnce)
        {
            items.ForEach(item => Decline(item, OrderReviewExpenseStatus.DeclinedOnce));
            outcome.Applied.Add(string.Format(DeclinedOnceFormat, items.Count));
        }
        else if (decision.ExpenseDecline == OrderReviewExpenseStatus.DeclinedFinal)
        {
            var affected = items
                .Concat(draft.Items.Where(item => OrderReviewReadiness.IsPending(item)
                    && item.ExpenseStatus == OrderReviewExpenseStatus.Unasked))
                .Distinct()
                .ToList();
            affected.ForEach(item => Decline(item, OrderReviewExpenseStatus.DeclinedFinal));
            outcome.Applied.Add(string.Format(DeclinedFinalFormat, affected.Count));
        }
    }

    private static async Task RefreshVerdictsAsync(
        List<OrderReviewDraftItem> items, PolicyCache policies, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var parts = item.ChosenParts is { Count: > 0 } chosen ? chosen : item.ProposedParts;
            if (parts.Count == 0)
            {
                item.SplitVerdict = null;
                continue;
            }

            await policies.EvaluateAsync(item, parts, cancellationToken);
        }
    }

    private static void Decline(OrderReviewDraftItem item, OrderReviewExpenseStatus status)
    {
        item.Expenses = [];
        item.ExpenseStatus = status;
    }

    private static Qualification? FindByName(IEnumerable<Qualification> qualifications, string name) =>
        qualifications.FirstOrDefault(qualification => !qualification.IsDeleted
            && qualification.Name.GetAllValues().Values.Any(value =>
                string.Equals(value?.Trim(), name, StringComparison.OrdinalIgnoreCase)));

    private static OrderReviewPart CopyPart(OrderReviewPart part) => new() { Start = part.Start, End = part.End, Name = part.Name };

    private static OrderReviewExpense CopyExpense(OrderReviewExpense expense) =>
        new() { Description = expense.Description, Amount = expense.Amount, Taxable = expense.Taxable };

    /// <summary>
    /// Member policies per executing group, read once per call, and the verdict of a split for an item, written to the item.
    /// </summary>
    private sealed class PolicyCache(IGroupMemberPolicyReader reader, DateOnly date)
    {
        private readonly Dictionary<Guid, IReadOnlyList<SchedulingPolicy>> _byGroup = new();

        public async Task<OrderSplitVerdict> EvaluateAsync(
            OrderReviewDraftItem item, IReadOnlyList<OrderReviewPart> parts, CancellationToken cancellationToken)
        {
            var groupId = OrderReviewReadiness.ExecutingGroupId(item);
            IReadOnlyList<SchedulingPolicy> memberPolicies = [];
            if (groupId.HasValue)
            {
                if (!_byGroup.TryGetValue(groupId.Value, out var cached))
                {
                    cached = await reader.GetMemberPoliciesAsync(groupId.Value, date, cancellationToken);
                    _byGroup[groupId.Value] = cached;
                }

                memberPolicies = cached;
            }

            var verdict = OrderSplitRuleEvaluator.Evaluate(parts.Max(FullDayOrderRule.PartHours), memberPolicies);
            item.SplitVerdict = verdict.Kind;
            item.SplitAllowedMembers = verdict.AllowedMembers;
            item.SplitMemberCount = verdict.MemberCount;
            return verdict;
        }
    }
}
