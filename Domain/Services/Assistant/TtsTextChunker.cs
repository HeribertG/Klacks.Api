// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Splits long text into synthesis-friendly chunks at sentence boundaries so TTS providers
/// with per-request limits can speak arbitrarily long assistant answers. Chunks are
/// contiguous substrings: concatenating them reproduces the original text exactly.
/// </summary>
/// <param name="text">The full text to split</param>
/// <param name="maxChunkLength">Maximum length of a single chunk in characters</param>
namespace Klacks.Api.Domain.Services.Assistant;

public static class TtsTextChunker
{
    private static readonly char[] SentenceEndings = ['.', '!', '?', '\n'];

    public static IReadOnlyList<string> Split(string text, int maxChunkLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxChunkLength, 0);

        if (string.IsNullOrEmpty(text) || text.Length <= maxChunkLength)
        {
            return [text];
        }

        var chunks = new List<string>();
        var start = 0;

        while (start < text.Length)
        {
            var remaining = text.Length - start;
            if (remaining <= maxChunkLength)
            {
                chunks.Add(text[start..]);
                break;
            }

            var window = text.AsSpan(start, maxChunkLength);
            var splitAt = window.LastIndexOfAny(SentenceEndings);

            if (splitAt < 0)
            {
                splitAt = window.LastIndexOf(' ');
            }

            var length = splitAt < 0 ? maxChunkLength : splitAt + 1;
            chunks.Add(text.Substring(start, length));
            start += length;
        }

        return chunks;
    }
}
