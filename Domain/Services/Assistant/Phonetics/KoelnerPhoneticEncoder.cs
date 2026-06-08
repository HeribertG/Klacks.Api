// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cologne phonetics (Kölner Phonetik) encoder for German. Maps a word to a digit
/// code so equally-sounding words share a code (e.g. "Meier"/"Meyer" → "67").
/// </summary>
namespace Klacks.Api.Domain.Services.Assistant.Phonetics;

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;

public sealed class KoelnerPhoneticEncoder : IPhoneticEncoder
{
    public string Encode(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        var s = Normalize(word);
        if (s.Length == 0)
            return string.Empty;

        var codes = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            char? prev = i > 0 ? s[i - 1] : null;
            char? next = i < s.Length - 1 ? s[i + 1] : null;
            codes.Append(CodeFor(s[i], prev, next, i == 0));
        }

        var dedup = new StringBuilder();
        char? last = null;
        foreach (var d in codes.ToString())
        {
            if (d != last)
                dedup.Append(d);
            last = d;
        }

        var result = new StringBuilder();
        for (int i = 0; i < dedup.Length; i++)
        {
            if (dedup[i] == '0' && i != 0)
                continue;
            result.Append(dedup[i]);
        }

        return result.ToString();
    }

    private static string Normalize(string word)
    {
        var sb = new StringBuilder();
        foreach (var ch in word.ToUpperInvariant())
        {
            char c = ch switch
            {
                'Ä' => 'A',
                'Ö' => 'O',
                'Ü' => 'U',
                'ß' => 'S',
                _ => ch,
            };
            if (c is >= 'A' and <= 'Z')
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static string CodeFor(char c, char? prev, char? next, bool isInitial)
    {
        switch (c)
        {
            case 'A': case 'E': case 'I': case 'J': case 'O': case 'U': case 'Y': return "0";
            case 'H': return string.Empty;
            case 'B': return "1";
            case 'P': return next == 'H' ? "3" : "1";
            case 'D': case 'T': return next is 'C' or 'S' or 'Z' ? "8" : "2";
            case 'F': case 'V': case 'W': return "3";
            case 'G': case 'K': case 'Q': return "4";
            case 'L': return "5";
            case 'M': case 'N': return "6";
            case 'R': return "7";
            case 'S': case 'Z': return "8";
            case 'C': return CodeForC(prev, next, isInitial);
            case 'X': return prev is 'C' or 'K' or 'Q' ? "8" : "48";
            default: return string.Empty;
        }
    }

    private static string CodeForC(char? prev, char? next, bool isInitial)
    {
        if (isInitial)
            return IsIn(next, "AHKLOQRUX") ? "4" : "8";
        if (prev is 'S' or 'Z')
            return "8";
        return IsIn(next, "AHKOQUX") ? "4" : "8";
    }

    private static bool IsIn(char? c, string set) => c.HasValue && set.IndexOf(c.Value) >= 0;
}
