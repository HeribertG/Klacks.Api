// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Resolves the effective warn/block enforcement mode for a compliance rule type
/// (<see cref="Klacks.Api.Domain.Constants.ComplianceRuleNames"/>), falling back to a global default
/// mode, and whether a supervisor may override a block.
/// </summary>
public interface IComplianceEnforcementResolver
{
    Task<RuleEnforcementMode> GetModeAsync(string ruleName);

    Task<bool> IsSupervisorOverrideAllowedAsync();
}
