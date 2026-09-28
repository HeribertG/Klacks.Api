// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// How a wizard run was applied. Direct materialises the tokens straight into the real plan (no scenario);
/// Scenario creates an AnalyseScenario what-if container. The direct path is the most common acceptance case.
/// </summary>
public enum WizardApplyKind
{
    Direct = 0,
    Scenario = 1
}
