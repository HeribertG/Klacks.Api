// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// Everything OrderReviewAnalyzer needs, already loaded: the open orders in the caller's scope, what still blocks
/// sealing each of them, the group proposals of OrderGroupPlanner, the names of all groups, the effective scheduling
/// policies of every executing group's members and the qualifications the name heuristic matches against.
/// </summary>
/// <param name="Orders">Open orders (status OriginalOrder) in the caller's scope, loaded with their group links and customer</param>
/// <param name="MissingRequirementsByOrderId">IOrderSealingService.CollectMissingRequirements per order</param>
/// <param name="ProposedGroupsByOrderId">Group proposals for orders without a group (only for full group access)</param>
/// <param name="UnassignableReasonsByOrderId">Why no group could be proposed for an order without a group</param>
/// <param name="GroupNamesById">Names of all groups, for the groups orders already belong to</param>
/// <param name="MemberPoliciesByGroupId">Effective scheduling policies of the members of each executing group</param>
/// <param name="Qualifications">All qualifications</param>
/// <param name="Language">Language the qualification names are shown in</param>
public sealed record OrderReviewAnalysisInput(
    IReadOnlyList<Shift> Orders,
    IReadOnlyDictionary<Guid, IReadOnlyList<string>> MissingRequirementsByOrderId,
    IReadOnlyDictionary<Guid, OrderGroupAssignment> ProposedGroupsByOrderId,
    IReadOnlyDictionary<Guid, string> UnassignableReasonsByOrderId,
    IReadOnlyDictionary<Guid, string> GroupNamesById,
    IReadOnlyDictionary<Guid, IReadOnlyList<SchedulingPolicy>> MemberPoliciesByGroupId,
    IReadOnlyList<Qualification> Qualifications,
    string? Language);
