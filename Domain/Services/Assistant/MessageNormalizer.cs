// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of truth for turning a raw chat message into the stable key under which every part of
/// the learning loop recognises "the same utterance": case-collector clusters, trajectory capture and the
/// correction endpoint all hash through here. Before this type existed the detector normalised and the
/// trajectory did not, so the two hashes of one and the same message never matched.
/// </summary>
namespace Klacks.Api.Domain.Services.Assistant;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class MessageNormalizer
{
    public const int HashLength = 16;

    private const char Space = ' ';

    private static readonly Regex WordPattern = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    /// <summary>
    /// Normalises a message: Unicode NFC, trimmed, lower-cased invariantly and every run of whitespace
    /// collapsed to a single space. Returns an empty string for a null or blank message.
    /// </summary>
    /// <param name="message">Raw user message as it arrived from the chat</param>
    public static string Normalize(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var normalized = message.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var pendingSpace = false;

        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(Space);
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Normalises the message and returns the first 16 hex characters of its SHA-256 digest. Two messages
    /// that differ only in casing, surrounding or inner whitespace produce the same key.
    /// </summary>
    /// <param name="message">Raw user message as it arrived from the chat</param>
    public static string Hash(string? message)
    {
        var normalized = Normalize(message);
        if (normalized.Length == 0)
        {
            return new string('0', HashLength);
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(digest)[..HashLength].ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Shortens a message to the excerpt that may be persisted: the whole message when it is short
    /// enough, otherwise its first sentence, otherwise a hard cut. The result never exceeds
    /// <paramref name="maxLength"/> characters, which is what keeps the promise that the learning loop
    /// stores an excerpt and never the message.
    /// </summary>
    /// <param name="message">Raw user message as it arrived from the chat</param>
    /// <param name="maxLength">Upper bound in characters, matching the width of the target column</param>
    public static string Excerpt(string? message, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var trimmed = message.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        var sentenceEnd = trimmed.IndexOfAny(['.', '!', '?', '\n']);
        if (sentenceEnd > 0 && sentenceEnd <= maxLength)
        {
            return trimmed[..sentenceEnd].Trim();
        }

        return trimmed[..maxLength].Trim();
    }

    /// <summary>
    /// Number of letter or digit runs in a message. The learning loop uses it as a floor: below a few
    /// words a refusal phrase matches noise far more often than a real capability wish.
    /// </summary>
    /// <param name="message">Raw user message as it arrived from the chat</param>
    public static int CountWords(string? message) =>
        string.IsNullOrWhiteSpace(message) ? 0 : WordPattern.Matches(message).Count;
}
