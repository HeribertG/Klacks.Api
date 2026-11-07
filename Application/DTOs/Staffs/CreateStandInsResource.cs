// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Request to create several stand-ins at once.
/// </summary>
/// <param name="Count">Number of stand-ins, 1 to StandInConstants.MaxBulkCount</param>
/// <param name="NamePattern">Display name with the index placeholder "{n}" exactly once, e.g. "Fair {n}"</param>
/// <param name="GroupId">Group every stand-in becomes a member of; it must be visible to the caller</param>
/// <param name="ValidFrom">First day of the membership and the group membership</param>
/// <param name="ValidUntil">Last day of both, or null for open-ended</param>
/// <param name="QualificationIds">Qualifications every stand-in holds (at StandInConstants.QualificationLevel)</param>

namespace Klacks.Api.Application.DTOs.Staffs;

public class CreateStandInsResource
{
    public int Count { get; set; }

    public string NamePattern { get; set; } = string.Empty;

    public Guid GroupId { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public List<Guid> QualificationIds { get; set; } = [];
}
