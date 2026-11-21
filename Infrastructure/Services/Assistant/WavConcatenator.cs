// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Joins RIFF/WAVE files of the same format into one: the PCM samples of all data chunks are appended in order and a
/// fresh header (RIFF size, fmt chunk of the first piece, data size) is written. A single piece is returned unchanged.
/// Pieces whose fmt chunks differ, or that lack a fmt or data chunk, are rejected with InvalidDataException.
/// </summary>
/// <param name="pieces">WAV files in playback order, as returned by Klacks.Stt</param>
using System.Buffers.Binary;
using System.Text;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public static class WavConcatenator
{
    private const int ChunkHeaderLength = 8;
    private const int RiffHeaderLength = 12;
    private const int ChunkSizeOffset = 4;
    private const int WaveIdOffset = 8;
    private const int IdLength = 4;
    private const int WordAlignment = 2;

    private static readonly byte[] RiffId = Encoding.ASCII.GetBytes("RIFF");
    private static readonly byte[] WaveId = Encoding.ASCII.GetBytes("WAVE");
    private static readonly byte[] FmtId = Encoding.ASCII.GetBytes("fmt ");
    private static readonly byte[] DataId = Encoding.ASCII.GetBytes("data");

    public static byte[] Concatenate(IReadOnlyList<byte[]> pieces)
    {
        ArgumentNullException.ThrowIfNull(pieces);
        if (pieces.Count == 0)
        {
            return [];
        }

        if (pieces.Count == 1)
        {
            return pieces[0];
        }

        var parsed = pieces.Select(Parse).ToList();
        var format = parsed[0].Format;
        if (parsed.Any(p => !p.Format.Span.SequenceEqual(format.Span)))
        {
            throw new InvalidDataException("The WAV pieces do not share the same format.");
        }

        var dataLength = parsed.Sum(p => (long)p.Data.Length);
        var formatPadding = format.Length % WordAlignment;
        var totalLength = RiffHeaderLength + ChunkHeaderLength + format.Length + formatPadding + ChunkHeaderLength + dataLength;
        if (totalLength > uint.MaxValue)
        {
            throw new InvalidDataException("The joined WAV file exceeds the RIFF size limit.");
        }

        using var output = new MemoryStream((int)Math.Min(totalLength, int.MaxValue));
        output.Write(RiffId);
        WriteUInt32(output, (uint)(totalLength - ChunkHeaderLength));
        output.Write(WaveId);
        output.Write(FmtId);
        WriteUInt32(output, (uint)format.Length);
        output.Write(format.Span);
        if (formatPadding != 0)
        {
            output.WriteByte(0);
        }

        output.Write(DataId);
        WriteUInt32(output, (uint)dataLength);
        foreach (var piece in parsed)
        {
            output.Write(piece.Data.Span);
        }

        return output.ToArray();
    }

    private static (ReadOnlyMemory<byte> Format, ReadOnlyMemory<byte> Data) Parse(byte[] wav)
    {
        if (wav.Length < RiffHeaderLength
            || !wav.AsSpan(0, IdLength).SequenceEqual(RiffId)
            || !wav.AsSpan(WaveIdOffset, IdLength).SequenceEqual(WaveId))
        {
            throw new InvalidDataException("The audio piece is not a RIFF/WAVE file.");
        }

        ReadOnlyMemory<byte>? format = null;
        ReadOnlyMemory<byte>? data = null;
        var offset = RiffHeaderLength;
        while (offset + ChunkHeaderLength <= wav.Length)
        {
            var id = wav.AsSpan(offset, IdLength);
            var size = BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(offset + ChunkSizeOffset, IdLength));
            var bodyStart = offset + ChunkHeaderLength;
            var available = wav.Length - bodyStart;
            var bodyLength = (int)Math.Min(size, (uint)available);

            if (id.SequenceEqual(FmtId))
            {
                format = wav.AsMemory(bodyStart, bodyLength);
            }
            else if (id.SequenceEqual(DataId))
            {
                data = wav.AsMemory(bodyStart, bodyLength);
            }

            offset = bodyStart + bodyLength + (bodyLength % WordAlignment);
        }

        if (format is null || data is null)
        {
            throw new InvalidDataException("The audio piece has no fmt or data chunk.");
        }

        return (format.Value, data.Value);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[IdLength];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }
}
