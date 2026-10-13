// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The password policy Program.cs applies to IdentityOptions.Password. Setup/Status publishes the effective
/// options (not these literals), so the setup form can show the rules before the user submits a password.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PasswordPolicyDefaults
{
    public const bool RequireDigit = true;
    public const int RequiredLength = 8;
    public const bool RequireLowercase = false;
    public const bool RequireUppercase = false;
    public const bool RequireNonAlphanumeric = true;
}
