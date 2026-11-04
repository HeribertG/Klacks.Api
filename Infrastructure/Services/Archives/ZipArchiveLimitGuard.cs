// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Zip-bomb check run before an archive is opened by a library or extracted to disk. The declared entry
/// sizes of a zip archive can lie, so every entry is actually decompressed into a counting sink: the
/// total is capped, the number of entries is capped, and optionally the decompressed/compressed ratio of
/// each entry above a size threshold is capped. Shared by the employee import (xlsx) and the marketplace
/// plugin installation.
/// </summary>
using System.IO.Compression;

namespace Klacks.Api.Infrastructure.Services.Archives;

public static class ZipArchiveLimitGuard
{
    private const int BufferSize = 81920;

    public static ZipArchiveLimitCheck Check(byte[] content, ZipArchiveLimits limits)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            if (archive.Entries.Count > limits.MaxEntries)
            {
                return new ZipArchiveLimitCheck(
                    ZipArchiveLimitViolation.TooManyEntries,
                    $"The archive has {archive.Entries.Count} entries, more than {limits.MaxEntries}.");
            }

            var buffer = new byte[BufferSize];
            long total = 0;

            foreach (var entry in archive.Entries)
            {
                using var entryStream = entry.Open();
                long entryTotal = 0;
                int read;
                while ((read = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += read;
                    entryTotal += read;
                    if (total > limits.MaxUnpackedBytes)
                    {
                        return new ZipArchiveLimitCheck(
                            ZipArchiveLimitViolation.UnpackedSizeExceeded,
                            $"The unpacked content exceeds {limits.MaxUnpackedBytes} bytes.");
                    }

                    if (ExceedsCompressionRatio(entry, entryTotal, limits))
                    {
                        return new ZipArchiveLimitCheck(
                            ZipArchiveLimitViolation.CompressionRatioExceeded,
                            $"An archive entry exceeds the compression ratio of {limits.MaxCompressionRatio}.");
                    }
                }
            }

            return ZipArchiveLimitCheck.WithinLimits();
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or NotSupportedException)
        {
            return new ZipArchiveLimitCheck(ZipArchiveLimitViolation.Unreadable, "The archive is not readable.", ex);
        }
    }

    private static bool ExceedsCompressionRatio(ZipArchiveEntry entry, long entryTotal, ZipArchiveLimits limits)
    {
        if (limits.MaxCompressionRatio is not { } maxRatio || entryTotal <= limits.RatioCheckThresholdBytes)
        {
            return false;
        }

        return entryTotal > entry.CompressedLength * maxRatio;
    }
}
