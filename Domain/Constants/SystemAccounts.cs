// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Well-known account identifiers seeded by the system rather than created through registration.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SystemAccounts
{
    /// <summary>
    /// Fixed id of the seeded "admin@test.com" account (see DefaultSeed.SeedData). Keyed on the id,
    /// not the email, because the email is renameable while the seeded row's id is not.
    /// </summary>
    public const string SeedAdminUserId = "672f77e8-e479-4422-8781-84d218377fb3";
}
