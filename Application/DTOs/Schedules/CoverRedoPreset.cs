// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Prefill for covering an absence again after its stale cover scenario was refused: taken from the absences the cover
/// itself added (scenario absences without a real twin of the same person, day and type), not from the scenario range,
/// which a swap chain or a source scenario widens. A value is null when it is not unique (several people or types) or
/// when the cover added no absence.
/// </summary>
/// <param name="ClientId">The absent person, when the cover added absences for exactly one person</param>
/// <param name="AbsenceId">The absence type, when exactly one person with exactly one type</param>
/// <param name="FromDate">First day of the absences the cover added</param>
/// <param name="UntilDate">Last day of the absences the cover added</param>
public sealed record CoverRedoPreset(Guid? ClientId, Guid? AbsenceId, DateOnly? FromDate, DateOnly? UntilDate)
{
    public static CoverRedoPreset Empty { get; } = new(null, null, null, null);
}
