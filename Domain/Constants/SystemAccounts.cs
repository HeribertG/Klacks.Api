// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    /// <summary>
    /// Password hash the seeded admin account is created with (see DefaultSeed.SeedData), i.e. the hash of the
    /// publicly documented default password. Identity replaces it on a password change and also on the first successful
    /// sign-in, because it was written with an older iteration count; a stored hash that still equals this value
    /// therefore means nobody has signed in with or changed the seeded password yet.
    /// </summary>
    public const string SeedAdminInitialPasswordHash =
        "AQAAAAEAACcQAAAAEM4rFqzwCkNDdqC7P5XDITL1ub4TLm1MPZMru7BlKyFLNSRfaamO4BUl/fAV4aNNlA==";
}
