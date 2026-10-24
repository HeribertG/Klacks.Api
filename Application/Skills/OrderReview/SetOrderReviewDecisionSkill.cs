// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Records the user's answers in the order review for one or more review keys or orders: the start of a full-day order,
/// the split, the group of an order without one, the qualifications, the expenses (or that there are none), taking the
/// remaining proposals, or leaving orders out. Every answer is validated (OrderReviewDecisionApplier); a refused answer
/// is reported with its reason and not stored. Returns what was stored, what was refused, hints, and the targeted keys
/// with what is still open. Draft-only; nothing in the domain changes.
/// </summary>
/// <param name="target">Required. Review key(s) such as B1 or S2, order id(s), comma separated, or 'all'.</param>
/// <param name="startTime">Optional. New start HH:mm of a full-day order (00:00 keeps midnight).</param>
/// <param name="split">Optional. '3x8', '2x12', 'none' or explicit parts 'HH:mm-HH:mm,...'.</param>
/// <param name="partNames">Optional. Names of the parts, comma separated, in part order.</param>
/// <param name="groupName">Optional. Group that staffs orders without a group.</param>
/// <param name="qualifications">Optional. 'none' or qualification names, comma separated.</param>
/// <param name="expenses">Optional. 'description:amount' or 'description:amount:taxable', several separated by ';'.</param>
/// <param name="noExpenses">Optional. 'this' (no expenses for the targeted orders) or 'all' (none for any order left).</param>
/// <param name="acceptProposals">Optional. true takes the proposed start, split, group and qualifications where undecided.</param>
/// <param name="skip">Optional. true leaves the targeted orders out of this review.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Orders.Review;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.OrderReview;

[SkillImplementation(SkillName)]
public class SetOrderReviewDecisionSkill : BaseSkillImplementation
{
    public const string SkillName = "set_order_review_decision";

    private const string NoReviewError = "No order review in progress. Start one with start_order_review first.";

    private const string StoredMessage =
        "Stored the answers. Relay what was refused and every hint, then ask for the next open aspect of the key; " +
        "when nothing is open any more, show preview_order_review for it.";

    private const string RefusedMessage =
        "Some answers were refused and NOT stored - explain the reason to the user and ask again.";

    private readonly IPendingOrderReviewDraftStore _draftStore;
    private readonly IOrderReviewLoader _loader;
    private readonly IOrderReviewDecisionApplier _applier;

    public SetOrderReviewDecisionSkill(
        IPendingOrderReviewDraftStore draftStore, IOrderReviewLoader loader, IOrderReviewDecisionApplier applier)
    {
        _draftStore = draftStore;
        _loader = loader;
        _applier = applier;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conversationKey = OrderReviewDraftScope.ConversationKey(context);
        var draft = _draftStore.Get(context.UserId, conversationKey);
        if (draft is null)
        {
            return SkillResult.Error(NoReviewError);
        }

        var (decision, parseError) = OrderReviewDecisionParser.Parse(parameters, context.UserLanguage);
        if (decision == null)
        {
            return SkillResult.Error(parseError!);
        }

        var (targets, targetError) = OrderReviewTargets.Resolve(draft, decision.Target);
        if (targetError != null)
        {
            return SkillResult.Error(targetError);
        }

        var scope = await _loader.ResolveScopeAsync(context, cancellationToken);
        if (scope.Refusal != null)
        {
            return SkillResult.Error(scope.Refusal);
        }

        var outcome = await _applier.ApplyAsync(draft, targets, decision, scope, context.UserLanguage, cancellationToken);
        _draftStore.Set(context.UserId, conversationKey, draft);

        var touchedKeys = targets.Select(item => item.ReviewKey).ToHashSet(StringComparer.Ordinal);
        var data = new
        {
            outcome.Applied,
            outcome.Rejected,
            outcome.Hints,
            Keys = OrderReviewPresenter.DescribeKeys(
                draft.Items.Where(item => touchedKeys.Contains(item.ReviewKey)), touchedKeys.Count),
        };

        return SkillResult.SuccessResult(data, outcome.Rejected.Count == 0 ? StoredMessage : RefusedMessage);
    }
}
