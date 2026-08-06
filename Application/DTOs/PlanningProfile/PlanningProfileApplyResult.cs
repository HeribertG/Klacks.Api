// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of applying a planning profile: the base choice used, the names of the customer-owned
/// scheduling rules that were created, how many were created, the parameter overrides that were applied
/// to each created rule, and the ACTIVE_INDUSTRIES value the installation now runs on (always the custom
/// marker).
/// </summary>

using System.Collections.Generic;

namespace Klacks.Api.Application.DTOs.PlanningProfile;

public sealed class PlanningProfileApplyResult
{
    public string BaseChoice { get; init; } = string.Empty;

    public int CreatedRuleCount { get; init; }

    public IReadOnlyList<string> CreatedRuleNames { get; init; } = new List<string>();

    public IReadOnlyDictionary<string, string> AppliedOverrides { get; init; } = new Dictionary<string, string>();

    public string ActiveIndustries { get; init; } = string.Empty;

    /// <summary>
    /// How many contracts still reference a rule of an industry that is no longer active. Applying a
    /// profile copies rules but reassigns no contract, so a non-zero value means the new rules are not
    /// in effect anywhere yet and the admin has to work through the migration list.
    /// </summary>
    public int ContractsAwaitingMigration { get; init; }
}
