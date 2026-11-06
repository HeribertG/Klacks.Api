// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats a folder path for display (Import card, assistant answers): the storage resolver returns a prefix
/// key such as erp/orders/ as a path with a trailing separator, which is correct for key semantics but reads as
/// noise in front of a user and invites a doubled separator when a file name is appended. Both separator styles are
/// handled whatever the host OS, because the path may describe a Windows host while the process runs on Linux.
/// A filesystem root (/ or C:\) keeps its separator.
/// </summary>
/// <param name="path">Absolute folder path as returned by the storage resolver or built from the host path hint</param>

namespace Klacks.Api.Domain.Services.Imports;

public static class ErpFolderDisplayPath
{
    private const char UnixSeparator = '/';
    private const char WindowsSeparator = '\\';
    private const char DriveSeparator = ':';
    private const int DriveRootLength = 3;
    private const int DriveSeparatorIndex = 1;

    public static string WithoutTrailingSeparator(string path)
    {
        var end = path.Length;
        while (end > 0 && IsSeparator(path[end - 1]))
        {
            end--;
        }

        if (end == 0)
        {
            return path.Length == 0 ? path : path[..1];
        }

        if (end == DriveSeparatorIndex + 1 && path[DriveSeparatorIndex] == DriveSeparator && path.Length >= DriveRootLength)
        {
            return path[..DriveRootLength];
        }

        return path[..end];
    }

    private static bool IsSeparator(char c) => c == UnixSeparator || c == WindowsSeparator;
}
