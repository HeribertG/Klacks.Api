// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>Window a team-fairness constraint compares over (stored by name in ParametersJson).</summary>
public enum PlanningFairnessWindow
{
    PlanPeriod = 1,
    Week = 2,
    Month = 3,
}
