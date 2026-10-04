// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Hard planning-rule findings before and after a wizard run, shown in the result so the user learns which violations
/// remain. In Wizard 2 and stage 3 the rule guard rejects every move that raises a person's summed excess of a hard
/// rule, so no hard rule gets worse in total; existing violations are not repaired and can shift within a rule. The
/// AutoWizard chain starts with Wizard 1, which does not honour planning rules yet, so its counts may rise. The
/// remaining findings are listed in the schedule error list.
/// </summary>
/// <param name="HardBefore">Hard findings of the plan the run started from</param>
/// <param name="HardAfter">Hard findings of the plan the run produced</param>
public sealed record PlanningRuleRemainingDto(int HardBefore, int HardAfter);
