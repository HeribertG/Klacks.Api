// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ComplianceEnforcement;

/// <summary>
/// What is known about one compliance rule before the change: its effective reaction and the findings it raised in the window.
/// </summary>
/// <param name="Rule">Rule name (ComplianceRuleNames)</param>
/// <param name="CurrentMode">The effective reaction of the rule, own setting or default mode</param>
/// <param name="WarningCount">Findings of the rule that are warnings now and would turn into errors under Block</param>
/// <param name="EnforcedErrorCount">Errors of the rule that exist because it is set to Block</param>
public sealed record ComplianceRuleFacts(
    string Rule,
    RuleEnforcementMode CurrentMode,
    int WarningCount,
    int EnforcedErrorCount);
