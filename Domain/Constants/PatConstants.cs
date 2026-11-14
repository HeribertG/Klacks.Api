// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants for personal access token generation, display, authentication and access modes.
/// An OAuth-issued token gets its mode from the consent (read-only unless the user chose write and the installation allows it).
/// </summary>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class PatConstants
{
    public const string TokenPrefix = "klacks_pat_";

    public const int TokenByteLength = 32;

    public const int DisplayPrefixLength = 12;

    public const string SchemeName = "KlacksPat";

    public const int DefaultExpiresInDays = 365;

    public const int MinExpiresInDays = 1;

    public const int MaxExpiresInDays = 730;

    public const string TokenIdClaimType = "klacks_pat_id";

    public const string AccessModeClaimType = "klacks_pat_access_mode";

    public const PersonalAccessTokenAccessMode DefaultAccessMode = PersonalAccessTokenAccessMode.Read;

    public static readonly TimeSpan LastUsedUpdateInterval = TimeSpan.FromMinutes(5);
}
