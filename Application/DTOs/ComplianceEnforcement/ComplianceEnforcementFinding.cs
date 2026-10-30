// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ComplianceEnforcement;

/// <summary>
/// One deterministic finding of the compliance enforcement change advisor: a code the tests pin and a server-built sentence.
/// </summary>
/// <param name="Code">Finding kind</param>
/// <param name="Rule">The rule the finding is about; null for a finding that concerns the whole evaluation</param>
/// <param name="Statement">Factual sentence</param>
public sealed record ComplianceEnforcementFinding(
    ComplianceEnforcementFindingCode Code,
    string? Rule,
    string Statement);
