// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Normalizes header words and gender/salutation tokens so that spelling variants compare equal:
/// compatibility form (full-width letters become ASCII), invariant lower case, accents of Latin and
/// Greek letters removed, and every character that is neither letter, digit nor combining mark of a
/// non-Latin script dropped (spaces, punctuation, symbols). "E-Mail", "e mail" and "ＥＭＡＩＬ" all become
/// "email"; Thai vowel signs or Japanese voicing marks are kept because they carry meaning there.
/// </summary>

using System.Globalization;
using System.Text;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportTextNormalizer
{
    private const int FirstNonLatinGreekCodePoint = 0x0400;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormKC)
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        char? lastBase = null;

        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (IsMark(category))
            {
                if (lastBase.HasValue && lastBase.Value < FirstNonLatinGreekCodePoint)
                {
                    continue;
                }

                builder.Append(character);
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastBase = character;
                continue;
            }

            lastBase = null;
        }

        return builder.ToString();
    }

    private static bool IsMark(UnicodeCategory category) =>
        category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;
}
