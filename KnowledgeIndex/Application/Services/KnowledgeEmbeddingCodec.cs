// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Buffers.Binary;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Converts embedding vectors and text hashes between their in-memory and snapshot representations.
/// Vectors are stored as base64 of little-endian float32 values, hashes as lowercase hex, so a
/// snapshot file written on one architecture reads identically on another.
/// </summary>
public static class KnowledgeEmbeddingCodec
{
    /// <summary>
    /// Encodes a vector as base64 of its little-endian float32 representation.
    /// </summary>
    /// <param name="vector">Embedding vector to encode.</param>
    public static string EncodeVector(float[] vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        var bytes = new byte[vector.Length * sizeof(float)];
        for (var i = 0; i < vector.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), vector[i]);
        }

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decodes a base64 little-endian float32 payload back into a vector.
    /// </summary>
    /// <param name="base64">Base64 payload produced by <see cref="EncodeVector"/>.</param>
    public static float[] DecodeVector(string base64)
    {
        ArgumentNullException.ThrowIfNull(base64);

        var bytes = Convert.FromBase64String(base64);
        if (bytes.Length % sizeof(float) != 0)
        {
            throw new FormatException("Embedding payload length is not a multiple of the float32 size.");
        }

        var vector = new float[bytes.Length / sizeof(float)];
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * sizeof(float)));
        }

        return vector;
    }

    /// <summary>
    /// Renders a hash as lowercase hex. The snapshot lookup key is compared ordinally, so the casing
    /// is part of the format.
    /// </summary>
    /// <param name="hash">Hash bytes to render.</param>
    public static string ToHex(byte[] hash)
    {
        ArgumentNullException.ThrowIfNull(hash);

        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Parses a hex string back into its bytes. Accepts either casing.
    /// </summary>
    /// <param name="hex">Hex string to parse.</param>
    public static byte[] FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        return Convert.FromHexString(hex);
    }
}
