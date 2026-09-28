// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Inputs of the arithmetic capacity check, read after all proposals of the plan.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;

namespace Klacks.Api.Application.Services.Grouping;

public sealed record CapacityInput(
    GroupingGroupTree Tree,
    GroupingMembershipState State,
    IReadOnlyDictionary<Guid, GroupingShiftRecord> Shifts,
    IReadOnlyDictionary<Guid, IReadOnlyList<DateOnly>> RunDays,
    IGroupingEligibilityOracle Eligibility,
    Guid? FocusGroupId);
