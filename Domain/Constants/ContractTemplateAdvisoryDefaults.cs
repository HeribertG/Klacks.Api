// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared defaults for the contract-from-template skills (evaluate_contract_templates and
/// create_contract_from_template). MaxCandidates caps how many ranked templates the advisory skill returns;
/// StandardValueLabel is how a null tri-state contract value (the shift-work flag) is shown to the caller;
/// NoOwnCalendarLabel names the holiday calendar of a template that has none of its own; InheritedWorkloadLabel
/// and NoPercentLabel describe the value a workload-path switch drops; DecimalFormat renders hour and percent
/// values without trailing zeros.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContractTemplateAdvisoryDefaults
{
    public const int MaxCandidates = 3;

    public const string StandardValueLabel = "standard";

    public const string NoOwnCalendarLabel = "company default calendar";

    public const string InheritedWorkloadLabel = "none (inherited from the company-wide value)";

    public const string NoPercentLabel = "none (fixed hours)";

    public const string DecimalFormat = "0.####";
}
