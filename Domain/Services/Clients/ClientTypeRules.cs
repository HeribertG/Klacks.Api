// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a client may take part in, decided by its type in one place. A stand-in is a placeholder for a person who is
/// not in the system yet: it can be planned like an employee, but it never takes over a replacement, it has no
/// target hours and the client import does not create it.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Clients;

public static class ClientTypeRules
{
    public static readonly IReadOnlyList<EntityTypeEnum> PlannableTypes =
    [
        EntityTypeEnum.Employee,
        EntityTypeEnum.ExternEmp,
        EntityTypeEnum.StandIn,
    ];

    public static readonly IReadOnlyList<EntityTypeEnum> ReplacementCandidateTypes =
    [
        EntityTypeEnum.Employee,
        EntityTypeEnum.ExternEmp,
    ];

    public static readonly IReadOnlyList<EntityTypeEnum> WithoutTargetHoursTypes =
    [
        EntityTypeEnum.StandIn,
    ];

    public static readonly IReadOnlyList<EntityTypeEnum> ImportableTypes =
    [
        EntityTypeEnum.Employee,
        EntityTypeEnum.ExternEmp,
        EntityTypeEnum.Customer,
    ];

    /// <summary>True when a Work can be planned on a client of this type.</summary>
    /// <param name="type">Type of the client</param>
    public static bool IsPlannable(EntityTypeEnum type) => PlannableTypes.Contains(type);

    /// <summary>True when a client of this type can take over a replacement.</summary>
    /// <param name="type">Type of the client</param>
    public static bool IsReplacementCandidate(EntityTypeEnum type) => ReplacementCandidateTypes.Contains(type);

    /// <summary>True when the client import may create a client of this type.</summary>
    /// <param name="type">Type of the client</param>
    public static bool IsImportable(EntityTypeEnum type) => ImportableTypes.Contains(type);
}
