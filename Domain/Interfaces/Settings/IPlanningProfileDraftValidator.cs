// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates a single planning-profile parameter value against its catalog definition and reports which
/// required parameters are still missing from a draft, including the conditional "at least one field
/// override when starting from scratch" rule.
/// </summary>

using System.Collections.Generic;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface IPlanningProfileDraftValidator
{
    PlanningProfileValidationResult Validate(string parameterName, string value);

    IReadOnlyList<string> GetMissingRequired(PlanningProfileDraft draft);
}
