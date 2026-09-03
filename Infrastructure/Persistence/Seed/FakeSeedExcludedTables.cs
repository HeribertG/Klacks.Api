// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Names the tables of the fake seed dump that must be dropped when demo data is restricted to
/// clients (no groups/shifts). Membership is the client's employment record (type, valid_from/until)
/// and carries no group/shift reference, so it is seeded regardless of this flag; group_item is the
/// client-to-group assignment and is excluded.
/// </summary>

namespace Klacks.Api.Data.Seed;

public static class FakeSeedExcludedTables
{
    public const string GroupItem = "group_item";

    public static IReadOnlySet<string> ShiftsAndGroupsTables { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { GroupItem };
}
