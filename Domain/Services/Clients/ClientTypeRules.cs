// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a client may take part in, decided by its type in one place. A stand-in is a placeholder for a person who is
/// not in the system yet: it can be planned like an employee, but it never takes over a replacement, it has no
/// target hours, the client import does not create it, it carries a display name only (no first name, gender, address,
/// communication or contract) and no client changes its type to or from a stand-in in place.
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

    public static readonly IReadOnlyList<EntityTypeEnum> StandInTypes =
    [
        EntityTypeEnum.StandIn,
    ];

    public static readonly IReadOnlyList<EntityTypeEnum> MasterDataTypes =
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

    /// <summary>True when the client is a stand-in: a display name only, no person master data.</summary>
    /// <param name="type">Type of the client</param>
    public static bool IsStandIn(EntityTypeEnum type) => StandInTypes.Contains(type);

    /// <summary>
    /// True when a client of type <paramref name="from"/> may be saved as <paramref name="to"/> in place: an unchanged type,
    /// or a change between two master data types. A change to or from a stand-in is refused.
    /// </summary>
    /// <param name="from">Stored type of the client</param>
    /// <param name="to">Requested type of the client</param>
    public static bool CanChangeType(EntityTypeEnum from, EntityTypeEnum to) =>
        from == to || (MasterDataTypes.Contains(from) && MasterDataTypes.Contains(to));

    /// <summary>
    /// Reads a master data type from its exact name (case-insensitive). A number, a combined value or a stand-in is not
    /// read.
    /// </summary>
    /// <param name="name">Type name supplied by a caller, e.g. a skill parameter</param>
    /// <param name="type">The type read, or Employee when nothing was read</param>
    public static bool TryParseMasterDataType(string? name, out EntityTypeEnum type)
    {
        var trimmed = name?.Trim();
        foreach (var candidate in MasterDataTypes)
        {
            if (string.Equals(candidate.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                type = candidate;
                return true;
            }
        }

        type = EntityTypeEnum.Employee;
        return false;
    }
}
