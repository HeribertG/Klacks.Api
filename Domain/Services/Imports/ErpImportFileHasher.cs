// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single hashing code path of the ERP import file ledger: SHA-256 of the raw bytes as lower-case hex.
/// Upload (hash while copying) and later verification (hash only) must produce byte-identical values,
/// so every object storage implementation goes through these two methods.
/// </summary>
/// <param name="content">Raw file bytes, read from the current position to the end</param>
/// <param name="destination">Stream the bytes are copied to while they are hashed</param>
/// <param name="maxBytes">Bounded variant only: content longer than this is not copied further (null result)</param>
using System.Buffers;
using System.Security.Cryptography;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Services.Imports;

public static class ErpImportFileHasher
{
    private const int CopyBufferSize = 81920;

    public static string ComputeSha256Hex(Stream content)
    {
        return Convert.ToHexStringLower(SHA256.HashData(content));
    }

    public static async Task<StoredObjectInfo> CopyAndHashAsync(Stream content, Stream destination, CancellationToken cancellationToken = default)
    {
        return (await CopyAndHashAsync(content, destination, long.MaxValue, cancellationToken))!;
    }

    /// <summary>
    /// Copies and hashes like CopyAndHashAsync, but stops as soon as the content turns out to be longer than
    /// maxBytes and returns null then -- the destination holds at most maxBytes bytes, so a caller buffering
    /// into memory stays bounded.
    /// </summary>
    /// <param name="maxBytes">Largest accepted content length</param>
    public static async Task<StoredObjectInfo?> CopyAndHashAsync(Stream content, Stream destination, long maxBytes, CancellationToken cancellationToken = default)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        long sizeBytes = 0;

        try
        {
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, CopyBufferSize), cancellationToken)) > 0)
            {
                if (read > maxBytes - sizeBytes)
                {
                    return null;
                }

                hash.AppendData(buffer, 0, read);
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                sizeBytes += read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return new StoredObjectInfo(sizeBytes, Convert.ToHexStringLower(hash.GetHashAndReset()));
    }
}
