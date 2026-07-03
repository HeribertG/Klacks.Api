// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Central definition of the object storage key layout used by the ERP order import: the
/// processed and error sub-segment names, bucket prefix normalization, upload key generation
/// and the mapping of a stored file name back to its display name.
/// </summary>
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Imports;

public static partial class ErpImportStorageKeys
{
    public const string ProcessedSegment = "processed";

    public const string ErrorSegment = "error";

    public const char KeySeparator = '/';

    private const string UploadIdPrefixPattern = "^[0-9a-fA-F]{32}-";

    public static string NormalizePrefix(string bucketPrefix)
    {
        return bucketPrefix.TrimEnd(KeySeparator) + KeySeparator;
    }

    public static string SegmentPrefix(string normalizedPrefix, string segment)
    {
        return normalizedPrefix + segment + KeySeparator;
    }

    public static string BuildUploadKey(string bucketPrefix, string fileName)
    {
        return $"{NormalizePrefix(bucketPrefix)}{Guid.NewGuid():N}-{fileName}";
    }

    public static string ToDisplayFileName(string storedFileName)
    {
        return UploadIdPrefixRegex().Replace(storedFileName, string.Empty);
    }

    [GeneratedRegex(UploadIdPrefixPattern)]
    private static partial Regex UploadIdPrefixRegex();
}
