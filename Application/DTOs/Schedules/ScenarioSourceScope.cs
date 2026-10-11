// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The scenario a flow builds on (scenario from scenario). Inside its own range the source scenario owns the whole plan
/// of its group's members and, for every other employee, the works on the shifts it cloned; everything else reads the
/// real plan, mirroring how the scenario is cloned.
/// </summary>
/// <param name="Token">Source scenario token</param>
/// <param name="FromDate">First day the source scenario owns</param>
/// <param name="UntilDate">Last day the source scenario owns</param>
/// <param name="GroupId">Group of the source scenario; null = it covers every employee</param>
/// <param name="OwnedShiftIds">The source scenario's shifts in both id spaces: its clone ids and their real root ids</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record ScenarioSourceScope(
    Guid Token, DateOnly FromDate, DateOnly UntilDate, Guid? GroupId, IReadOnlySet<Guid> OwnedShiftIds)
{
    public bool OwnsDate(DateOnly date) => date >= FromDate && date <= UntilDate;
}
