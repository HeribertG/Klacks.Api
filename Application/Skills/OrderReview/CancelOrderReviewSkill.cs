// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Cancels the order review in progress by clearing its draft. Orders the review already executed stay changed, sealed
/// and cut; the result names how many. No-op friendly: reports when there was no review to cancel. Draft-only.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.OrderReview;

[SkillImplementation(SkillName)]
public class CancelOrderReviewSkill : BaseSkillImplementation
{
    public const string SkillName = "cancel_order_review";

    private const string CancelledFormat =
        "Cancelled the order review and discarded its draft. {0} order(s) the review already sealed stay as they are.";

    private const string NothingToCancel = "There was no order review in progress.";

    private readonly IPendingOrderReviewDraftStore _draftStore;

    public CancelOrderReviewSkill(IPendingOrderReviewDraftStore draftStore)
    {
        _draftStore = draftStore;
    }

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conversationKey = OrderReviewDraftScope.ConversationKey(context);
        var draft = _draftStore.Get(context.UserId, conversationKey);
        if (draft is null)
        {
            return Task.FromResult(SkillResult.SuccessResult(new { Cancelled = false }, NothingToCancel));
        }

        _draftStore.Clear(context.UserId, conversationKey);
        var executed = draft.Items.Count(item => item.ExecutionStatus == OrderReviewExecutionStatus.Executed);
        return Task.FromResult(SkillResult.SuccessResult(
            new { Cancelled = true, ExecutedCount = executed },
            string.Format(CancelledFormat, executed)));
    }
}
