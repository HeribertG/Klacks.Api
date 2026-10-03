// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>Metric a team-fairness constraint compares across the agents of its group (stored by name in ParametersJson).</summary>
public enum PlanningFairnessMetric
{
    NightDays = 1,
    WeekendDays = 2,
    WorkedDays = 3,
}
