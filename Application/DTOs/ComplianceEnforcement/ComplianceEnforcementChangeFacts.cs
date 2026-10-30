// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ComplianceEnforcement;

/// <summary>
/// The gathered facts about switching rules to Block, the input of ComplianceEnforcementChangeAdvisor.
/// </summary>
/// <param name="WindowStart">First day of the checked window</param>
/// <param name="WindowEnd">Last day of the checked window</param>
/// <param name="SupervisorOverrideAllowed">Whether a supervisor may push a refused assignment through</param>
/// <param name="IssueCapReached">True when the validation list hit its cap, so later days of the window are missing</param>
/// <param name="CoveredUntil">Date of the last finding the capped list reached; null when the list was not capped</param>
/// <param name="Rules">One entry per evaluated rule</param>
public sealed record ComplianceEnforcementChangeFacts(
    DateOnly WindowStart,
    DateOnly WindowEnd,
    bool SupervisorOverrideAllowed,
    bool IssueCapReached,
    DateOnly? CoveredUntil,
    IReadOnlyList<ComplianceRuleFacts> Rules);
