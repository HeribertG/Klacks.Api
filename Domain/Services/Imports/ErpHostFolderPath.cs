// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the host-side path of a drop point folder on a Docker installation: the host folder that the compose file
/// binds onto the storage root, followed by the drop point's bucket prefix. The separator follows the host: a hint
/// with a drive letter or a backslash is a Windows host (Docker Desktop), everything else uses forward slashes.
/// </summary>
/// <param name="hostPathHint">Host folder bound onto the storage root (ErpObjectStorage:HostPathHint); blank = unknown</param>
/// <param name="normalizedPrefix">Bucket prefix as normalized by ErpImportStorageKeys.NormalizePrefix, e.g. erp/orders/</param>

namespace Klacks.Api.Domain.Services.Imports;

public static class ErpHostFolderPath
{
    private const char UnixSeparator = '/';
    private const char WindowsSeparator = '\\';
    private const char DriveSeparator = ':';
    private const int DriveSeparatorIndex = 1;

    public static string? Combine(string? hostPathHint, string normalizedPrefix)
    {
        if (string.IsNullOrWhiteSpace(hostPathHint))
        {
            return null;
        }

        var hint = hostPathHint.Trim();
        var separator = IsWindowsPath(hint) ? WindowsSeparator : UnixSeparator;
        var root = hint.Replace(UnixSeparator, separator).Replace(WindowsSeparator, separator).TrimEnd(separator);
        var relative = normalizedPrefix
            .Replace(WindowsSeparator, separator)
            .Replace(ErpImportStorageKeys.KeySeparator, separator)
            .Trim(separator);

        return relative.Length == 0 ? root : root + separator + relative;
    }

    private static bool IsWindowsPath(string path)
    {
        var hasDriveLetter = path.Length > DriveSeparatorIndex
            && char.IsAsciiLetter(path[0])
            && path[DriveSeparatorIndex] == DriveSeparator;

        return hasDriveLetter || path.Contains(WindowsSeparator);
    }
}
