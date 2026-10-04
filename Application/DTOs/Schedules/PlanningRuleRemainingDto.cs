// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Hard planning-rule findings before and after a wizard run, shown in the result so the user learns which violations
/// remain. The rule guard only rejects moves that worsen a rule, so a run does not add violations, but it does not
/// repair existing ones either; the remaining ones are listed in the schedule error list.
/// </summary>
/// <param name="HardBefore">Hard findings of the plan the run started from</param>
/// <param name="HardAfter">Hard findings of the plan the run produced</param>
public sealed record PlanningRuleRemainingDto(int HardBefore, int HardAfter);
