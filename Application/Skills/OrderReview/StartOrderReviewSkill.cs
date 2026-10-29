// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Starts the guided review of the open orders ("go through the open orders with me"): loads the open orders in the
/// caller's scope, analyses them (proposed start 07:00 for full-day orders, the standard 3x8 split judged against the
/// rules of the group members, the group, qualifications by name) and bundles them by pattern, outliers one by one. The
/// review is stored as a draft (replacing one already in progress) and the overview is returned with the instruction
/// to go through the review keys one at a time. Draft-only: nothing in the domain changes until execute_order_review
/// runs as the confirmed replay of its preview. A started review carries a top-level Route to the shift list with the
/// open orders preselected (view=orders), which the chat opens after the turn; a scope without open orders does not.
/// </summary>
/// <param name="customerName">Optional. Restricts the review to the orders of customers whose name contains this text.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Orders.Review;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.OrderReview;

[SkillImplementation(SkillName)]
public class StartOrderReviewSkill : BaseSkillImplementation
{
    public const string SkillName = "start_order_review";
    public const string CustomerNameParameter = "customerName";

    public const int MaxKeysInOverview = 30;

    private const string NoOpenOrdersMessage =
        "There are no open orders in your scope - nothing to review. Nothing was stored.";

    private const string GuideMessage =
        "Started the review of {0} open order(s) in {1} review key(s){2}. Go through the keys one at a time, the " +
        "largest bundle first; a bundle (B..) shares one pattern and is decided once, a single (S..) is an outlier " +
        "with its reason. For each key ask, one question at a time: (1) the start of a full-day order - propose the " +
        "shown start and explain its coverage (with 07:00 the first day stays uncovered from midnight to 07:00 and " +
        "the last service runs to 07:00 the next day); the user may also keep midnight. Changing the start changes " +
        "the order itself - say so. (2) the split - the standard is 3x8 with the shown rule verdict; offer 2x12 only " +
        "if the user asks, and when the rules allow a split only for some members, say so. (3) the group, with why " +
        "it is proposed. (4) the qualifications, if any are proposed. (5) always ask whether there are expenses " +
        "(every part of the cut carries them). Record each answer with set_order_review_decision, then show " +
        "preview_order_review for the key and run execute_order_review only after the user confirms it. The shift list " +
        "is being opened on the open orders, so the user sees them while you go through them.";

    private const string ReplacedNote = ", replacing the review that was in progress";

    private readonly IOrderReviewLoader _loader;
    private readonly IPendingOrderReviewDraftStore _draftStore;

    public StartOrderReviewSkill(IOrderReviewLoader loader, IPendingOrderReviewDraftStore draftStore)
    {
        _loader = loader;
        _draftStore = draftStore;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var scope = await _loader.ResolveScopeAsync(context, cancellationToken);
        if (scope.Refusal != null)
        {
            return SkillResult.Error(scope.Refusal);
        }

        var items = await _loader.LoadItemsAsync(
            context, scope, GetParameter<string>(parameters, CustomerNameParameter), cancellationToken);
        if (items.Count == 0)
        {
            return SkillResult.SuccessResult(new { TotalOrders = 0 }, NoOpenOrdersMessage);
        }

        var conversationKey = OrderReviewDraftScope.ConversationKey(context);
        var replaced = _draftStore.Get(context.UserId, conversationKey) is not null;
        _draftStore.Set(context.UserId, conversationKey, new OrderReviewDraft { Items = items });

        var keyCount = items.Select(item => item.ReviewKey).Distinct(StringComparer.Ordinal).Count();
        var data = new
        {
            TotalOrders = items.Count,
            KeyCount = keyCount,
            BundleCount = items.Where(item => item.OutlierReason == null)
                .Select(item => item.ReviewKey).Distinct(StringComparer.Ordinal).Count(),
            SingleCount = items.Count(item => item.OutlierReason != null),
            MaxOrdersPerExecution = OrderReviewDefaults.MaxOrdersPerExecution,
            Keys = OrderReviewPresenter.DescribeKeys(items, MaxKeysInOverview),
            Route = ShiftListViewRoutes.OrdersViewRoute,
        };

        return SkillResult.SuccessResult(
            data,
            string.Format(GuideMessage, items.Count, keyCount, replaced ? ReplacedNote : string.Empty));
    }
}
