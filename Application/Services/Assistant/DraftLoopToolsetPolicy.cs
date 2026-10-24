// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The toolset rules of the open draft loops of a conversation, moved out of SkillToolsetGuaranteeResolver. A planning-
/// profile draft keeps its loop skills in the toolset: the answers that continue it ("security", "8.5") carry no
/// keyword, and without them the model reached for create_scheduling_rule (observed live 2026-08-10). An order-review
/// draft keeps its loop skills in the toolset and keeps out every skill that would change, seal, cut or regroup the
/// orders past the review's own confirmed preview (update_shift, seal_shift, seal_open_orders, cut_shift,
/// cut_shift_by_date, assign_orders_to_groups, add_shift_to_group). Each loop is keyed by the same scope its skills
/// write the draft under; the order review is probed with HasDraft, which does not load the (possibly large) draft.
/// A store failure degrades to no rule for that loop instead of taking toolset assembly down. Visibility only: a step skill
/// a recipe guarantees (add_shift_to_group in add-shift-to-group) stays exempt from the exclusion, and every excluded skill
/// still runs through the autonomy gate.
/// </summary>
/// <param name="planningProfileDraftStore">Tells whether a planning-profile setup is open in the conversation.</param>
/// <param name="orderReviewDraftStore">Tells whether an order review is open in the conversation.</param>
/// <param name="logger">Logs a failing store read.</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Services.Orders.Review;
using Klacks.Api.Application.Skills;
using Klacks.Api.Application.Skills.OrderReview;
using Klacks.Api.Application.Skills.PlanningProfile;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public sealed class DraftLoopToolsetPolicy : IDraftLoopToolsetPolicy
{
    public static readonly IReadOnlyList<string> PlanningProfileLoopSkillNames =
    [
        "set_planning_profile_parameters",
        "preview_planning_profile",
        "apply_planning_profile",
        "cancel_planning_profile_setup"
    ];

    public static readonly IReadOnlyList<string> OrderReviewLoopSkillNames =
    [
        SetOrderReviewDecisionSkill.SkillName,
        PreviewOrderReviewSkill.SkillName,
        ExecuteOrderReviewSkill.SkillName,
        CancelOrderReviewSkill.SkillName
    ];

    public static readonly IReadOnlySet<string> OrderReviewExcludedSkillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "update_shift",
        SealShiftSkill.SkillName,
        "seal_open_orders",
        "cut_shift",
        "cut_shift_by_date",
        "assign_orders_to_groups",
        "add_shift_to_group"
    };

    private readonly IPendingPlanningProfileDraftStore _planningProfileDraftStore;
    private readonly IPendingOrderReviewDraftStore _orderReviewDraftStore;
    private readonly ILogger<DraftLoopToolsetPolicy> _logger;

    public DraftLoopToolsetPolicy(
        IPendingPlanningProfileDraftStore planningProfileDraftStore,
        IPendingOrderReviewDraftStore orderReviewDraftStore,
        ILogger<DraftLoopToolsetPolicy> logger)
    {
        _planningProfileDraftStore = planningProfileDraftStore;
        _orderReviewDraftStore = orderReviewDraftStore;
        _logger = logger;
    }

    public DraftLoopToolset Resolve(string userId, string? conversationId)
    {
        if (!Guid.TryParse(userId, out var draftUserId))
        {
            return DraftLoopToolset.None;
        }

        var guaranteed = new List<string>();
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (IsOpen("Planning-profile", () =>
                _planningProfileDraftStore.Get(draftUserId, PlanningProfileDraftScope.ConversationKey(conversationId)) is not null))
        {
            guaranteed.AddRange(PlanningProfileLoopSkillNames);
        }

        if (IsOpen("Order-review", () =>
                _orderReviewDraftStore.HasDraft(draftUserId, OrderReviewDraftScope.ConversationKey(conversationId))))
        {
            guaranteed.AddRange(OrderReviewLoopSkillNames);
            excluded.UnionWith(OrderReviewExcludedSkillNames);
        }

        return new DraftLoopToolset(guaranteed, excluded);
    }

    private bool IsOpen(string loopName, Func<bool> probe)
    {
        try
        {
            return probe();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Loop} draft lookup failed; continuing without its toolset rule.", loopName);
            return false;
        }
    }
}
