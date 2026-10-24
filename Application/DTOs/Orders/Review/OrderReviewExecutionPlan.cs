// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// What a confirmed execute_order_review would do, or why it cannot run - shared by the skill and its confirmation
/// preview so the user confirms exactly what is executed.
/// </summary>
/// <param name="Refusal">Why nothing can be executed; null for a runnable plan</param>
/// <param name="Draft">The review draft the targets belong to</param>
/// <param name="Targets">The ready orders of this confirmation (at most OrderReviewDefaults.MaxOrdersPerExecution)</param>
/// <param name="MoreReady">Ready orders left for the next confirmation</param>
/// <param name="NotReadyCount">Targeted orders that still have open aspects</param>
/// <param name="Binding">The facts the confirmation is bound to (ConfirmationBindingKinds.OrderReview)</param>
/// <param name="Text">The preview text shown to the user</param>
public sealed record OrderReviewExecutionPlan(
    string? Refusal,
    OrderReviewDraft? Draft,
    IReadOnlyList<OrderReviewExecutionTarget> Targets,
    int MoreReady,
    int NotReadyCount,
    ConfirmationBinding? Binding,
    string Text)
{
    public bool IsRefused => Refusal != null;

    public static OrderReviewExecutionPlan Refuse(string refusal) => new(refusal, null, [], 0, 0, null, string.Empty);
}
