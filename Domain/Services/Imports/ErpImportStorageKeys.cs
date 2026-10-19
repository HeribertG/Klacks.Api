// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Central definition of the object storage key layout used by the ERP order import: bucket prefix
/// normalization, upload key generation (unique "{guid}-{name}" keys, also given to files an ERP drops into
/// the inbox under its own name) and the mapping of a stored file name back to its display name. Every file
/// stays at its inbox key; the pre-ledger sub-folder names live in ErpImportLegacyFolders.
/// </summary>
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Imports;

public static partial class ErpImportStorageKeys
{
    public const char KeySeparator = '/';

    private const string UploadIdPrefixPattern = "^[0-9a-fA-F]{32}-";

    public static string NormalizePrefix(string bucketPrefix)
    {
        return bucketPrefix.TrimEnd(KeySeparator) + KeySeparator;
    }

    public static string BuildUploadKey(string bucketPrefix, string fileName)
    {
        return $"{NormalizePrefix(bucketPrefix)}{Guid.NewGuid():N}-{fileName}";
    }

    /// <summary>
    /// True when the stored file name already starts with the "{32 hex}-" upload id that BuildUploadKey
    /// prepends, i.e. the key is already unique and needs no renaming.
    /// </summary>
    public static bool HasUploadIdPrefix(string storedFileName)
    {
        return UploadIdPrefixRegex().IsMatch(storedFileName);
    }

    public static string ToDisplayFileName(string storedFileName)
    {
        return UploadIdPrefixRegex().Replace(storedFileName, string.Empty);
    }

    [GeneratedRegex(UploadIdPrefixPattern)]
    private static partial Regex UploadIdPrefixRegex();
}
