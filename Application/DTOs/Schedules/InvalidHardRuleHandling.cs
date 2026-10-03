// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// What a planning-rule load does with an approved Hard constraint whose stored parameters are invalid. Engines
/// fail closed (Throw); the validators report it as a finding of its own and keep evaluating the valid rules
/// (Report), so one broken row never blocks every write.
/// </summary>
public enum InvalidHardRuleHandling
{
    Throw = 1,
    Report = 2,
}
