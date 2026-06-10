// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum SkillRiskClass
{
    ReadOnly = 0,
    Reversible = 1,
    ScenarioGated = 2,
    Irreversible = 3,
    Sensitive = 4
}
