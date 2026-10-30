// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ComplianceEnforcement;

/// <summary>
/// The result of evaluate_compliance_enforcement_change: countable effects per rule, the findings and the
/// server-computed recommendation, which the model relays and does not rephrase into a different conclusion.
/// Counts only, no names of people.
/// </summary>
/// <param name="WindowStart">First day of the checked window</param>
/// <param name="WindowEnd">Last day of the checked window</param>
/// <param name="SupervisorOverrideAllowed">Whether a supervisor may push a refused assignment through</param>
/// <param name="CountsAreLowerBounds">True when the counts are "at least": the validation list was cut off before the window end</param>
/// <param name="CoveredUntil">Last day the counts reach when they are lower bounds; null otherwise</param>
/// <param name="TotalWarningsTurningIntoErrors">Sum over the evaluated rules</param>
/// <param name="Rules">The effect per evaluated rule, in catalog order</param>
/// <param name="Findings">Every finding</param>
/// <param name="Recommendation">The computed recommendation</param>
public sealed record ComplianceEnforcementChangeEvaluation(
    DateOnly WindowStart,
    DateOnly WindowEnd,
    bool SupervisorOverrideAllowed,
    bool CountsAreLowerBounds,
    DateOnly? CoveredUntil,
    int TotalWarningsTurningIntoErrors,
    IReadOnlyList<ComplianceRuleEffect> Rules,
    IReadOnlyList<ComplianceEnforcementFinding> Findings,
    string Recommendation);
