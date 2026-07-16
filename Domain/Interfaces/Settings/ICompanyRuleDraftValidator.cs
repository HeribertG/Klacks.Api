// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates a single company-rule parameter value against its catalog definition and reports which
/// required parameters are still missing from a draft, including the conditional hours-threshold rule.
/// </summary>

using System.Collections.Generic;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ICompanyRuleDraftValidator
{
    CompanyRuleValidationResult Validate(CompanyRuleKind kind, string parameterName, string value);

    IReadOnlyList<string> GetMissingRequired(CompanyRuleDraft draft);
}
