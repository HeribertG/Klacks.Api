// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Generates the v2 Sec-MS-GEC token ("&lt;16 hex CS64&gt;-&lt;64 hex SHA-256&gt;") required by Microsoft Edge's
/// online TTS WebSocket since October 2026; the v1 plain SHA-256 token is rejected with HTTP 403.
/// Port of edge-tts 7.3.1 (drm.py, generate_sec_ms_gec_v2).
/// </summary>
/// <param name="unixSeconds">Current UTC time in Unix seconds; rounded down to a 5-minute window</param>
using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public static class EdgeTtsSecMsGecGenerator
{
    public const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

    private const long WindowsEpochOffsetSeconds = 11644473600L;
    private const long TokenWindowSeconds = 300L;
    private const long WindowsTicksPerSecond = 10_000_000L;
    private const int HiddenPrefixLength = 57;
    private const int InitDataLength = 7;
    private const int BlockSize = 64;
    private const int RoundCount = 64;
    private const int ResumeRound = 14;
    private const uint Sha256Adjustment = 0xC9DC06A7;
    private const uint Sha256Sigma0Adjustment = 0x059C0B80;
    private const uint Sha256Sigma1Adjustment = 0x000B92C0;
    private const uint Cs64Factor = 0x82893933;
    private const uint Cs64Adjustment0 = 0xF2F90B1A;
    private const uint Cs64Adjustment1 = 0x0B1A0000;

    private static readonly uint[] Sha256Initial =
    {
        0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A, 0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19,
    };

    private static readonly uint[] Sha256State =
    {
        0xFD9FBEA2, 0x91243CE7, 0xBE499094, 0xCD2E1E0A, 0x6C006F49, 0x04BD35AB, 0xFEB0A7E4, 0x03EFFBD2,
    };

    private static readonly uint[] Sha256Schedule =
    {
        0xEB24BF9E, 0x1558691F, 0xA0C09EE2, 0x0CFA32BA, 0xA622EE8C, 0xD1DD9491, 0xC0DD25A0,
        0xF9E70AB6, 0xF0C31573, 0x7058CE4E, 0x7188E606, 0x00E5AB33, 0xD258B060, 0x72766564,
    };

    private static readonly uint[] Sha256RoundConstants =
    {
        0x428A2F98, 0x71374491, 0xB5C0FBCF, 0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
        0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74, 0x80DEB1FE, 0x9BDC06A7, 0xC19BF174,
        0xE49B69C1, 0xEFBE4786, 0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC, 0x76F988DA,
        0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7, 0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967,
        0x27B70A85, 0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB, 0x81C2C92E, 0x92722C85,
        0xA2BFE8A1, 0xA81A664B, 0xC24B8B70, 0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
        0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3, 0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3,
        0x748F82EE, 0x78A5636F, 0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7, 0xC67178F2,
    };

    private static readonly uint[] Sha256BlockConstants = BuildBlockConstants();

    private static readonly (uint Coefficient, uint Offset)[] Cs64Coefficients =
    {
        (0xE82E91B1, 0xB32700D0),
        (0x44DE24C6, 0x88DFF51D),
        (0x3BEB65BC, 0x29514038),
        (0xC89C5933, 0xB328CBDF),
        (0x8D1EC211, 0xD405B4D5),
        (0x314EF5D5, 0x26E55F3C),
        (0xE92491B1, 0x607801C6),
        (0xF0A5B9D5, 0x82B2C1FB),
        (0x06064CF3, 0xFA51A3DA),
        (0xDA79FA60, 0x207CC464),
        (0xBED20560, 0x3C3C74F3),
        (0x89F9C9F8, 0xD2D14B61),
        (0x35FA38C6, 0xBB04A2C1),
        (0xCB49E3C6, 0xC5626167),
    };

    public static string Generate(long unixSeconds)
    {
        var windowsSeconds = unixSeconds + WindowsEpochOffsetSeconds;
        windowsSeconds -= windowsSeconds % TokenWindowSeconds;
        var ticks = windowsSeconds * WindowsTicksPerSecond;

        var data = Encoding.ASCII.GetBytes(ticks.ToString(CultureInfo.InvariantCulture) + TrustedClientToken);
        var digest = Sha256Digest(data);
        var (cs64Low, cs64High) = Cs64Digest(data, digest);

        var cs64Bytes = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(cs64Bytes.AsSpan(0, 4), cs64Low);
        BinaryPrimitives.WriteUInt32LittleEndian(cs64Bytes.AsSpan(4, 4), cs64High);

        return $"{Convert.ToHexString(cs64Bytes)}-{Convert.ToHexString(digest)}";
    }

    private static uint[] BuildBlockConstants()
    {
        var constants = (uint[])Sha256RoundConstants.Clone();
        constants[ResumeRound] = Sha256Adjustment;
        return constants;
    }

    private static int PositiveModulo(int value, int modulus) => ((value % modulus) + modulus) % modulus;

    private static uint SmallSigma0(uint value) =>
        BitOperations.RotateRight(value, 7) ^ BitOperations.RotateRight(value, 18) ^ (value >> 3);

    private static uint SmallSigma1(uint value) =>
        BitOperations.RotateRight(value, 17) ^ BitOperations.RotateRight(value, 19) ^ (value >> 10);

    private static uint Rol16(uint value) => BitOperations.RotateLeft(value, 16);

    private static void ExpandSchedule(uint[] words, int firstWord)
    {
        for (var index = firstWord; index < RoundCount; index++)
        {
            words[index] = unchecked(SmallSigma1(words[index - 2]) + words[index - 7]
                + SmallSigma0(words[index - 15]) + words[index - 16]);
        }
    }

    private static uint[] RunRounds(uint[] state, uint[] words, int firstRound)
    {
        var constants = firstRound == ResumeRound ? Sha256BlockConstants : Sha256RoundConstants;
        uint a = state[0], b = state[1], c = state[2], d = state[3];
        uint e = state[4], f = state[5], g = state[6], h = state[7];

        for (var index = firstRound; index < RoundCount; index++)
        {
            unchecked
            {
                var bigSigma1 = BitOperations.RotateRight(e, 6) ^ BitOperations.RotateRight(e, 11) ^ BitOperations.RotateRight(e, 25);
                var choose = g ^ (e & (f ^ g));
                var t1 = h + bigSigma1 + choose + constants[index] + words[index];
                var bigSigma0 = BitOperations.RotateRight(a, 2) ^ BitOperations.RotateRight(a, 13) ^ BitOperations.RotateRight(a, 22);
                var majority = (a & b) | (c & (a | b));
                var t2 = bigSigma0 + majority;
                h = g;
                g = f;
                f = e;
                e = d + t1;
                d = c;
                c = b;
                b = a;
                a = t1 + t2;
            }
        }

        return new[] { a, b, c, d, e, f, g, h };
    }

    private static uint[] AddStates(uint[] left, uint[] right)
    {
        var result = new uint[left.Length];
        for (var index = 0; index < left.Length; index++)
        {
            result[index] = unchecked(left[index] + right[index]);
        }

        return result;
    }

    private static uint[] Sha256Init(ReadOnlySpan<byte> data)
    {
        var words = new uint[RoundCount];
        words[14] = ((uint)data[0] << 16) | ((uint)data[1] << 8) | data[2];
        words[15] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(3, 4));

        for (var index = 16; index < 30; index++)
        {
            unchecked
            {
                var value = Sha256Schedule[index - 16];
                if (index - 2 >= ResumeRound)
                {
                    var sigma1 = SmallSigma1(words[index - 2]);
                    value += index == 16 ? sigma1 ^ Sha256Sigma1Adjustment : sigma1;
                }

                if (index - 7 >= ResumeRound)
                {
                    value += words[index - 7];
                }

                if (index - 15 >= ResumeRound)
                {
                    var sigma0 = SmallSigma0(words[index - 15]);
                    value += index == 29 ? sigma0 ^ Sha256Sigma0Adjustment : sigma0;
                }

                words[index] = value;
            }
        }

        words[30] = unchecked(SmallSigma1(words[28]) + words[23] + SmallSigma0(words[15]) + words[14]
            + Sha256Adjustment - Sha256RoundConstants[ResumeRound]);
        ExpandSchedule(words, 31);

        return AddStates(Sha256Initial, RunRounds(Sha256State, words, ResumeRound));
    }

    private static byte[] Sha256Digest(byte[] data)
    {
        if (data.Length < InitDataLength)
        {
            throw new ArgumentException("Token salt must contain at least seven bytes", nameof(data));
        }

        var length = HiddenPrefixLength + data.Length;
        var tailLength = data.Length - InitDataLength;
        var zeroPadding = PositiveModulo(55 - length, BlockSize);
        var padded = new byte[tailLength + 1 + zeroPadding + sizeof(ulong)];
        data.AsSpan(InitDataLength).CopyTo(padded);
        padded[tailLength] = 0x80;
        BinaryPrimitives.WriteUInt64BigEndian(padded.AsSpan(padded.Length - sizeof(ulong)), (ulong)length * 8);

        var state = Sha256Init(data.AsSpan(0, InitDataLength));
        for (var offset = 0; offset < padded.Length; offset += BlockSize)
        {
            var words = new uint[RoundCount];
            for (var index = 0; index < 16; index++)
            {
                words[index] = BinaryPrimitives.ReadUInt32BigEndian(padded.AsSpan(offset + index * 4, 4));
            }

            ExpandSchedule(words, 16);
            state = AddStates(state, RunRounds(state, words, 0));
        }

        var digest = new byte[state.Length * 4];
        for (var index = 0; index < state.Length; index++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(digest.AsSpan(index * 4, 4), state[index]);
        }

        return digest;
    }

    private static uint[] LittleEndianWordsEvenCount(byte[] data)
    {
        var padded = new byte[data.Length + PositiveModulo(-data.Length, 4)];
        data.CopyTo(padded, 0);
        var wordCount = padded.Length / 4;
        var words = new uint[wordCount + wordCount % 2];
        for (var index = 0; index < wordCount; index++)
        {
            words[index] = BinaryPrimitives.ReadUInt32LittleEndian(padded.AsSpan(index * 4, 4));
        }

        return words;
    }

    private static (uint Low, uint High) Cs64Digest(byte[] data, byte[] digest)
    {
        unchecked
        {
            var keyA = BinaryPrimitives.ReadUInt32LittleEndian(digest.AsSpan(0, 4)) | 1;
            var keyB = BinaryPrimitives.ReadUInt32LittleEndian(digest.AsSpan(4, 4)) | 1;
            uint[] factors = { keyA * Cs64Factor, keyB * Cs64Factor };

            var terms = new List<uint>();
            var offsets = new List<uint>();
            for (var index = 0; index < Cs64Coefficients.Length; index++)
            {
                terms.Add(Cs64Coefficients[index].Coefficient * factors[index % 2]);
                offsets.Add(Cs64Coefficients[index].Offset);
            }

            var prefixed = new byte[data.Length + 1];
            data.CopyTo(prefixed, 1);
            var words = LittleEndianWordsEvenCount(prefixed);
            for (var index = 0; index < words.Length; index++)
            {
                var word = words[index];
                var term = word * (index % 2 == 0 ? keyA : keyB);
                var offset = Rol16(word) * (index % 2 == 0 ? 0xEF0569FBu : 0xC31713DBu);
                if (index == 0)
                {
                    term += Cs64Adjustment0 * factors[0];
                    offset += Cs64Adjustment1;
                }

                terms.Add(term);
                offsets.Add(offset);
            }

            uint swapAcc = 0, swapLast = 0, state = 0, acc = 0;
            for (var index = 0; index < terms.Count; index += 2)
            {
                var x = terms[index];
                var y = terms[index + 1];

                var q = Rol16(offsets[index] + x) * 0x76F50F34u;
                swapAcc += Rol16(q) * 0xC3EFEA97u + q;
                q = Rol16(offsets[index + 1] + y) * 0x1881CE3Cu;
                swapLast = Rol16(q) * 0x35BD1EC9u + q;
                swapAcc += swapLast;

                state = state * keyA + x;
                state = Rol16(state) * 0xCF98B111u;
                state = Rol16(state) * 0x87085B9Fu;
                state = Rol16(state) * 0x12CEB96Du;
                state = Rol16(state) * 0x257E1D83u;
                acc += state;
                var t = state * keyB + y;
                t = Rol16(t) * 0xA27416F5u;
                t = Rol16(t) * 0xD38396FFu;
                state = Rol16(Rol16(t) * 0x7C932B89u) * 0xBFA49F69u;
                acc += state;
            }

            return (swapLast ^ state, swapAcc ^ acc);
        }
    }
}
