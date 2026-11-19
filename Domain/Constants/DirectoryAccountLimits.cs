// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Limits of the directory approval list: rows per answer, search term length and external id length.
/// </summary>
public static class DirectoryAccountLimits
{
    public const int ListLimit = 1000;

    public const int SearchMaxLength = 100;

    public const int ExternalIdMaxLength = 500;
}
