// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Zip-bomb check for xlsx uploads, run before ClosedXML opens the file. The declared entry sizes of a
/// zip archive can lie, so every entry is actually decompressed into a counting sink and the total is
/// capped; the number of entries is capped as well.
/// </summary>

using System.IO.Compression;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services.ClientImport;

public static class ClientImportZipGuard
{
    private const int BufferSize = 81920;

    public static void EnsureWithinLimits(byte[] content, long maxUnpackedBytes, int maxEntries)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            if (archive.Entries.Count > maxEntries)
            {
                throw Reject($"The archive has {archive.Entries.Count} entries, more than {maxEntries}.");
            }

            var buffer = new byte[BufferSize];
            long total = 0;

            foreach (var entry in archive.Entries)
            {
                using var entryStream = entry.Open();
                int read;
                while ((read = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += read;
                    if (total > maxUnpackedBytes)
                    {
                        throw Reject($"The unpacked content exceeds {maxUnpackedBytes} bytes.");
                    }
                }
            }
        }
        catch (ClientImportRejectedException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or NotSupportedException)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileUnsupported, "The file is not a readable xlsx archive.", ex);
        }
    }

    private static ClientImportRejectedException Reject(string message) =>
        new(ClientImportErrorCodes.UnpackedSizeExceeded, message);
}
