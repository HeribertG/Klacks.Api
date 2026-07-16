// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Supplies the parameter definitions the assistant may collect for each company-rule kind, so the
/// intake dialog and the validator share a single source of truth for names, types and constraints.
/// </summary>

using System.Collections.Generic;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ICompanyRuleParameterCatalog
{
    IReadOnlyList<CompanyRuleParameterDefinition> GetParameters(CompanyRuleKind kind);

    CompanyRuleParameterDefinition? FindParameter(CompanyRuleKind kind, string parameterName);
}
