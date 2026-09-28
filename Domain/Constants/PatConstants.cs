// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants for personal access token generation, display and authentication.
/// </summary>
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

    public static readonly TimeSpan LastUsedUpdateInterval = TimeSpan.FromMinutes(5);
}
