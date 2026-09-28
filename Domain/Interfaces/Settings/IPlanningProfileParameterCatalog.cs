// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Supplies the parameter definitions the assistant may collect for the planning-profile setup, so the
/// setup dialog and the validator share a single source of truth for names, types, constraints and the
/// plain-language meaning and planning impact used to explain each answer.
/// </summary>

using System.Collections.Generic;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface IPlanningProfileParameterCatalog
{
    IReadOnlyList<PlanningProfileParameterDefinition> GetParameters();

    PlanningProfileParameterDefinition? FindParameter(string parameterName);
}
