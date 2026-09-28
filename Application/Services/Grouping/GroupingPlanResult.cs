// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Output of one plan build: findings F1-F6, the ordered proposals, the tree including a virtual new
/// group if one is proposed, and the membership state after all proposals, which CapacityEstimator reads.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Services.Grouping;

public sealed record GroupingPlanResult(
    IReadOnlyList<GroupingFinding> Findings,
    IReadOnlyList<GroupingProposal> Proposals,
    GroupingGroupTree Tree,
    GroupingMembershipState State);
