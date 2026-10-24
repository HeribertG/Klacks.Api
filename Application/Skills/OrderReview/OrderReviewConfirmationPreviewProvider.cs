// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of execute_order_review: when the autonomy gate holds the call, this provider plans it with the
/// very planner the skill uses (IOrderReviewExecutionPlanner) and shows the text - the section "Changes to orders" (a new
/// start changes the order itself), group, qualifications, expenses, then the sealing and the cut per order, every part
/// carrying the expenses. It refuses, so the gate issues no token, when nothing is ready, a start change is not
/// acknowledged or not allowed for the caller, or an order changed since the review started. The binding is the plan's
/// OrderReview binding: the confirmed replay executes exactly these orders with exactly these decisions or nothing.
/// It only reads.
/// </summary>
/// <param name="planner">Plans the execution from the review draft without writing.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.OrderReview;

public class OrderReviewConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private readonly IOrderReviewExecutionPlanner _planner;

    public OrderReviewConfirmationPreviewProvider(IOrderReviewExecutionPlanner planner)
    {
        _planner = planner;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, ExecuteOrderReviewSkill.SkillName, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planner.PlanAsync(context, parameters, cancellationToken);
        return plan.IsRefused
            ? SkillConfirmationPreview.Refuse(plan.Refusal!)
            : SkillConfirmationPreview.Show(plan.Text, plan.Binding!);
    }
}
