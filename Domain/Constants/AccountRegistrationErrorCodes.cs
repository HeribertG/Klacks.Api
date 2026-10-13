// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Codes of a rejected account registration (HTTP 400, "errorCode" plus a "reasons" array). The reasons are
/// ASP.NET Identity's own error codes, so they are taken from IdentityErrorDescriber by name; an Identity code
/// not listed here is passed through unchanged and the UI falls back to a generic text. The UI mirrors every
/// value in account-registration.constants.ts (AccountRegistrationErrorCodesUiMirrorGuardTests).
/// </summary>

using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Domain.Constants;

public static class AccountRegistrationErrorCodes
{
    public const string ErrorCode = "ACCOUNT_REGISTRATION_REJECTED";

    public const string PasswordTooShort = nameof(IdentityErrorDescriber.PasswordTooShort);
    public const string PasswordRequiresDigit = nameof(IdentityErrorDescriber.PasswordRequiresDigit);
    public const string PasswordRequiresNonAlphanumeric = nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric);
    public const string PasswordRequiresUpper = nameof(IdentityErrorDescriber.PasswordRequiresUpper);
    public const string PasswordRequiresLower = nameof(IdentityErrorDescriber.PasswordRequiresLower);
    public const string PasswordRequiresUniqueChars = nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars);
    public const string DuplicateEmail = nameof(IdentityErrorDescriber.DuplicateEmail);
    public const string DuplicateUserName = nameof(IdentityErrorDescriber.DuplicateUserName);
    public const string InvalidEmail = nameof(IdentityErrorDescriber.InvalidEmail);
    public const string InvalidUserName = nameof(IdentityErrorDescriber.InvalidUserName);
}
