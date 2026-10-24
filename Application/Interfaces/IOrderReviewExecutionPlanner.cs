// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Plans an execute_order_review call without writing anything, so the skill and its confirmation preview work from the
/// very same plan: the ready orders of the target, their binding and the preview text, or why nothing can run.
/// </summary>
public interface IOrderReviewExecutionPlanner
{
    Task<OrderReviewExecutionPlan> PlanAsync(
        SkillExecutionContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}
