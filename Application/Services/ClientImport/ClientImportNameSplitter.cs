// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Splits a full-name cell into first and last name. "Müller, Hans" is always last-first. Otherwise the
/// chosen order decides: first-last takes the last word (together with surname particles such as
/// "van der", "Von" or "De" right before it, in any case) as last name, last-first takes the first word. More than two
/// words is reported as multi-word so the user checks the split.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportNameSplitter
{
    private const char Comma = ',';
    private const int SimpleNameWordCount = 2;

    private static readonly HashSet<string> SurnameParticles = new(StringComparer.OrdinalIgnoreCase)
    {
        "van", "von", "der", "den", "de", "del", "della", "di", "da", "dos", "das", "du", "le", "la", "ten", "ter", "zu", "af", "bin", "binti"
    };

    public static ClientImportNameParts Split(string? fullName, ClientImportNameOrder order)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new ClientImportNameParts(null, null, false);
        }

        var commaIndex = fullName.IndexOf(Comma);
        if (commaIndex >= 0)
        {
            var last = fullName[..commaIndex].Trim();
            var first = fullName[(commaIndex + 1)..].Trim();
            return new ClientImportNameParts(NullIfEmpty(first), NullIfEmpty(last), false);
        }

        var words = fullName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
        {
            return new ClientImportNameParts(null, words[0], false);
        }

        var multiWord = words.Length > SimpleNameWordCount;

        if (order == ClientImportNameOrder.LastFirst)
        {
            return new ClientImportNameParts(string.Join(' ', words[1..]), words[0], multiWord);
        }

        var lastStart = words.Length - 1;
        while (lastStart > 1 && SurnameParticles.Contains(words[lastStart - 1]))
        {
            lastStart--;
        }

        return new ClientImportNameParts(string.Join(' ', words[..lastStart]), string.Join(' ', words[lastStart..]), multiWord);
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
