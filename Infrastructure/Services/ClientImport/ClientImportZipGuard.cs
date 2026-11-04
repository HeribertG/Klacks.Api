// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Zip-bomb check for xlsx uploads, run before ClosedXML opens the file. Delegates the counting to the
/// shared <see cref="ZipArchiveLimitGuard"/> (no ratio check: xlsx XML compresses very well) and maps its
/// verdict onto the employee-import error codes.
/// </summary>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Services.Archives;

namespace Klacks.Api.Infrastructure.Services.ClientImport;

public static class ClientImportZipGuard
{
    public static void EnsureWithinLimits(byte[] content, long maxUnpackedBytes, int maxEntries)
    {
        var check = ZipArchiveLimitGuard.Check(content, new ZipArchiveLimits(maxUnpackedBytes, maxEntries));

        if (check.IsWithinLimits)
        {
            return;
        }

        if (check.Violation == ZipArchiveLimitViolation.Unreadable)
        {
            throw new ClientImportRejectedException(
                ClientImportErrorCodes.FileUnsupported,
                "The file is not a readable xlsx archive.",
                check.Cause!);
        }

        throw new ClientImportRejectedException(ClientImportErrorCodes.UnpackedSizeExceeded, check.Message);
    }
}
