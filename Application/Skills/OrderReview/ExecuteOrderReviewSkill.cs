// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Carries out the confirmed part of the order review: for every ready order of the target (at most
/// OrderReviewDefaults.MaxOrdersPerExecution per confirmation) the order is changed as decided, sealed into its
/// plannable shift and cut into the decided parts, each order in its own transaction (IOrderReviewExecutor); a failed
/// order is rolled back, reported with its reason and stays open in the review, the others go on. It runs only as the
/// confirmed replay of its server preview (OrderReviewConfirmationPreviewProvider): without a confirmed binding of kind
/// OrderReview - a plan step, an accepted goal, MCP or any other path that bypasses the autonomy gate - it refuses before
/// it reads anything, and when the orders or decisions differ from the confirmed preview it refuses with the binding
/// mismatch text. The draft records what became of every order, so the review can be resumed; once no order of the
/// review is left to do, the draft is cleared, which also ends the toolset exclusions of the open review.
/// </summary>
/// <param name="target">Required. Review key(s) such as B1 or S2, order id(s), comma separated, or 'all'.</param>
/// <param name="acknowledgedOrderChange">true when the user confirmed that the start of an order changes.</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Orders.Review;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.OrderReview;

[SkillImplementation(SkillName)]
public class ExecuteOrderReviewSkill : BaseSkillImplementation
{
    public const string SkillName = "execute_order_review";

    private const string ResultFormat =
        "Executed the order review for {0} order(s): {1} changed, sealed and cut and confirmed in the database (verified), " +
        "{2} failed and rolled back (reasons per order). {3} ready order(s) wait for the next confirmation, {4} still have " +
        "open questions. Relay the counts and every failure reason, then continue with the next review key.";

    private readonly IOrderReviewExecutionPlanner _planner;
    private readonly IOrderReviewExecutor _executor;
    private readonly IPendingOrderReviewDraftStore _draftStore;

    public ExecuteOrderReviewSkill(
        IOrderReviewExecutionPlanner planner, IOrderReviewExecutor executor, IPendingOrderReviewDraftStore draftStore)
    {
        _planner = planner;
        _executor = executor;
        _draftStore = draftStore;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (context.ConfirmedActionBinding is not { Kind: ConfirmationBindingKinds.OrderReview } confirmed)
        {
            return SkillResult.Error(
                string.Format(CultureInfo.InvariantCulture, ConfirmationBindingMessages.BindingRequired, SkillName));
        }

        var plan = await _planner.PlanAsync(context, parameters, cancellationToken);
        if (plan.IsRefused)
        {
            return SkillResult.Error(plan.Refusal!);
        }

        if (!confirmed.Matches(plan.Binding!))
        {
            return SkillResult.Error(ConfirmationBindingMessages.Describe(confirmed, plan.Binding!));
        }

        var outcomes = new List<OrderReviewExecutionOutcome>();
        var conversationKey = OrderReviewDraftScope.ConversationKey(context);
        foreach (var target in plan.Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = await _executor.ExecuteAsync(target, context.UserName, context.UserLanguage, cancellationToken);
            target.Item.ExecutionStatus = outcome.Succeeded ? OrderReviewExecutionStatus.Executed : OrderReviewExecutionStatus.Failed;
            target.Item.FailureReason = outcome.FailureReason;
            target.Item.PlannableShiftId = outcome.PlannableShiftId;
            _draftStore.Set(context.UserId, conversationKey, plan.Draft!);
            outcomes.Add(outcome);
        }

        if (!plan.Draft!.Items.Any(OrderReviewReadiness.IsPending))
        {
            _draftStore.Clear(context.UserId, conversationKey);
        }

        var succeeded = outcomes.Count(outcome => outcome.Succeeded);
        var data = new
        {
            Executed = succeeded,
            Failed = outcomes.Count - succeeded,
            Verified = succeeded > 0,
            Orders = plan.Targets.Zip(outcomes, (target, outcome) => new
            {
                target.Item.OrderId,
                target.Item.OrderName,
                outcome.Succeeded,
                outcome.PlannableShiftId,
                outcome.PartCount,
                outcome.FailureReason,
            }).ToList(),
            plan.MoreReady,
            NotReady = plan.NotReadyCount,
        };

        return SkillResult.SuccessResult(data, string.Format(
            CultureInfo.InvariantCulture, ResultFormat, outcomes.Count, succeeded, outcomes.Count - succeeded,
            plan.MoreReady, plan.NotReadyCount));
    }
}
