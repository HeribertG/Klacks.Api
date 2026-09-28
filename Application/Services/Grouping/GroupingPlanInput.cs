// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Inputs of one plan build.
/// </summary>
/// <param name="Clients">Analysed employees and extern employees with a membership in the period.</param>
/// <param name="Shifts">Plannable shifts that run at least once in the period.</param>
/// <param name="Tree">Group hierarchy.</param>
/// <param name="Memberships">Real, non-scenario group_item rows.</param>
/// <param name="FutureWorkPairs">(client, shift) pairs with a real work from today on.</param>
/// <param name="Eligibility">Static eligibility per pair.</param>
/// <param name="FocusGroupId">Optional subtree the analysis is restricted to.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;

namespace Klacks.Api.Application.Services.Grouping;

public sealed record GroupingPlanInput(
    IReadOnlyList<GroupingClientRecord> Clients,
    IReadOnlyList<GroupingShiftRecord> Shifts,
    GroupingGroupTree Tree,
    IReadOnlyList<GroupingMembershipRecord> Memberships,
    IReadOnlySet<GroupingEntityPair> FutureWorkPairs,
    IGroupingEligibilityOracle Eligibility,
    Guid? FocusGroupId);
