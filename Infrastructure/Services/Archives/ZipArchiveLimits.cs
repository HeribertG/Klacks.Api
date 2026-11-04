// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits a zip archive must stay within before it is opened by a library or extracted to disk.
/// </summary>
/// <param name="MaxUnpackedBytes">Cap on the actually decompressed bytes of all entries together</param>
/// <param name="MaxEntries">Cap on the number of entries</param>
/// <param name="MaxCompressionRatio">Cap on decompressed/compressed bytes per entry; null disables the ratio check</param>
/// <param name="RatioCheckThresholdBytes">Entries that decompress to at most this many bytes are not ratio-checked, so small highly repetitive files pass</param>
namespace Klacks.Api.Infrastructure.Services.Archives;

public sealed record ZipArchiveLimits(
    long MaxUnpackedBytes,
    int MaxEntries,
    int? MaxCompressionRatio = null,
    long RatioCheckThresholdBytes = 0);
