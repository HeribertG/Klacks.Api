// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shows the order review for one or more review keys (default: every key with orders still to do): the decisions and
/// proposals per key, what is still open, and for the orders that are ready the text of what execute_order_review
/// would do - the changes to the orders (a new start changes the order itself), the sealing and the cut with the
/// expenses every part carries - at most OrderReviewDefaults.MaxOrdersPerExecution orders per confirmation. Reads the
/// draft only.
/// </summary>
/// <param name="target">Optional. Review key(s) such as B1 or S2, order id(s), comma separated, or 'all'.</param>

using Klacks.Api.Application.Services.Orders.Review;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.OrderReview;

[SkillImplementation(SkillName)]
public class PreviewOrderReviewSkill : BaseSkillImplementation
{
    public const string SkillName = "preview_order_review";
    public const string TargetParameter = "target";

    private const string NoReviewError = "No order review in progress. Start one with start_order_review first.";

    private const string ReadyMessage =
        "Show the user the text under WhatExecuteDoes, including the changes to the orders. When the user confirms, call " +
        "execute_order_review with the same target and acknowledgedOrderChange=true if the text changes an order's start.";

    private const string NothingReadyMessage =
        "No order of this target is ready yet. Ask for the open aspects listed per key.";

    private readonly IPendingOrderReviewDraftStore _draftStore;

    public PreviewOrderReviewSkill(IPendingOrderReviewDraftStore draftStore)
    {
        _draftStore = draftStore;
    }

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var draft = _draftStore.Get(context.UserId, OrderReviewDraftScope.ConversationKey(context));
        if (draft is null)
        {
            return Task.FromResult(SkillResult.Error(NoReviewError));
        }

        var target = GetParameter<string>(parameters, TargetParameter);
        var (targets, targetError) = string.IsNullOrWhiteSpace(target)
            ? (draft.Items.Where(OrderReviewReadiness.IsPending).ToList(), null)
            : OrderReviewTargets.Resolve(draft, target);
        if (targetError != null)
        {
            return Task.FromResult(SkillResult.Error(targetError));
        }

        var (ready, moreReady, notReady) = OrderReviewExecutionText.SelectReady(targets);
        var keys = targets.Select(item => item.ReviewKey).ToHashSet(StringComparer.Ordinal);
        var data = new
        {
            Keys = OrderReviewPresenter.DescribeKeys(targets, keys.Count),
            ReadyCount = ready.Count,
            MoreReady = moreReady,
            NotReadyCount = notReady.Count,
            WhatExecuteDoes = ready.Count > 0 ? OrderReviewExecutionText.Describe(ready, moreReady) : null,
        };

        return Task.FromResult(SkillResult.SuccessResult(data, ready.Count > 0 ? ReadyMessage : NothingReadyMessage));
    }
}
