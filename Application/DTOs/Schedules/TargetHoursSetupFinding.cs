// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One deterministic finding of the target-hours setup advisor: a code the tests pin and a server-built sentence
/// filled only with figures and master-data names.
/// </summary>
/// <param name="Code">Finding kind</param>
/// <param name="Statement">Factual sentence about the setup</param>
/// <param name="WhereToChange">Place in Klacks where the responsible value lives, when a change is advised</param>
public sealed record TargetHoursSetupFinding(
    TargetHoursSetupFindingCode Code,
    string Statement,
    string? WhereToChange);
