// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reduces a client-supplied file name to its last path segment (Windows or Unix separators, whatever
/// the server OS) within the length the batch record stores.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportFileName
{
    private static readonly char[] PathSeparators = ['/', '\\'];

    public static string Sanitize(string? fileName)
    {
        var value = fileName ?? string.Empty;
        var name = value[(value.LastIndexOfAny(PathSeparators) + 1)..].Trim();
        return name.Length > ClientImportLimits.MaxFileNameLength ? name[..ClientImportLimits.MaxFileNameLength] : name;
    }
}
