// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Identifies which wizard engine produced a captured run. TokenEvolution is Wizard 1 (GA planner),
/// Harmonizer is Wizard 2, Holistic is Wizard 3, Wizard4 is the composite optimizer. Feeds the
/// (deferred) preference-learner so it can group scores by the engine that produced them.
/// </summary>
public enum WizardEngine
{
    TokenEvolution = 0,
    Harmonizer = 1,
    Holistic = 2,
    Wizard4 = 3
}
