// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Judges one part length of a split order against the scheduling rules of the members of the group that will staff
/// it. Rules hang on each person's contract, so every member's effective policy is checked on its own: a part must not
/// exceed the maximum daily hours and the daily work frame, and the rest of the day (24h minus the part) must reach the
/// minimum rest. All members pass: Allowed; some: AllowedForSome (offered with a hint); none: NotAllowed; no members to
/// judge by: NoMembers. Pure.
/// </summary>

using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderSplitRuleEvaluator
{
    public static OrderSplitVerdict Evaluate(decimal partHours, IReadOnlyCollection<SchedulingPolicy> memberPolicies)
    {
        if (memberPolicies.Count == 0)
        {
            return new OrderSplitVerdict(OrderSplitVerdictKind.NoMembers, 0, 0, partHours);
        }

        var allowed = memberPolicies.Count(policy => Fits(partHours, policy));
        var kind = allowed == memberPolicies.Count
            ? OrderSplitVerdictKind.Allowed
            : allowed == 0 ? OrderSplitVerdictKind.NotAllowed : OrderSplitVerdictKind.AllowedForSome;

        return new OrderSplitVerdict(kind, allowed, memberPolicies.Count, partHours);
    }

    private static bool Fits(decimal partHours, SchedulingPolicy policy)
    {
        var hours = (double)partHours;
        return hours <= policy.MaxDailyHours.TotalHours
            && hours <= policy.DailyWorkFrame.TotalHours
            && OrderReviewDefaults.HoursPerDay - hours >= policy.MinRestHours.TotalHours;
    }
}
