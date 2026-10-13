// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Process-wide holder of the generated collision table, the one the static conversation-signal detectors consult.
/// ConversationSignalsPluginLoader registers every language's signals at startup; the holder merges them per language
/// under one lock and rebuilds the table from the merged whole, so registering the same language twice, or in several
/// batches, ends in the same table. An empty holder behaves like the detectors did before the table existed: a word
/// both a yes and a no stays a no.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ConversationSignalCollisions
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, ConversationLanguageSignals> ByLanguage =
        new(StringComparer.OrdinalIgnoreCase);

    private static volatile ConversationSignalCollisionTable _current = ConversationSignalCollisionTable.Empty;

    public static ConversationSignalCollisionTable Current => _current;

    public static void Register(IEnumerable<ConversationLanguageSignals> signals)
    {
        lock (Gate)
        {
            foreach (var incoming in signals)
            {
                ByLanguage[incoming.Language] = ByLanguage.TryGetValue(incoming.Language, out var existing)
                    ? Merge(existing, incoming)
                    : incoming;
            }

            _current = ConversationSignalCollisionTable.Build(ByLanguage.Values);
        }
    }

    /// <summary>
    /// The affirmation entries the registered pack of one UI language lists, empty for a language without a pack (the
    /// core languages de/en/fr/it have none) or for an unknown or blank one. A regional tag falls back to its base
    /// language ("pt-BR" reads the "pt" pack) unless the pack of the regional code itself is registered ("zh-CN").
    /// </summary>
    /// <param name="language">UI language of the turn; null when unknown</param>
    public static IReadOnlyList<string> AffirmationsOf(string? language)
    {
        var tag = language?.Trim();
        if (string.IsNullOrEmpty(tag))
        {
            return [];
        }

        lock (Gate)
        {
            if (ByLanguage.TryGetValue(tag, out var exact))
            {
                return exact.Affirmations;
            }

            var baseLanguage = LanguageTag.BaseLanguage(tag);
            return baseLanguage != null && ByLanguage.TryGetValue(baseLanguage, out var fallback)
                ? fallback.Affirmations
                : [];
        }
    }

    internal static void Reset()
    {
        lock (Gate)
        {
            ByLanguage.Clear();
            _current = ConversationSignalCollisionTable.Empty;
        }
    }

    private static ConversationLanguageSignals Merge(ConversationLanguageSignals left, ConversationLanguageSignals right) =>
        new(left.Language)
        {
            Affirmations = Union(left.Affirmations, right.Affirmations),
            Negations = Union(left.Negations, right.Negations),
            Declines = Union(left.Declines, right.Declines),
            Cancellations = Union(left.Cancellations, right.Cancellations),
            Corrections = Union(left.Corrections, right.Corrections),
            GapIndicators = Union(left.GapIndicators, right.GapIndicators)
        };

    private static IReadOnlyList<string> Union(IReadOnlyList<string> left, IReadOnlyList<string> right) =>
        left.Concat(right).Distinct(StringComparer.Ordinal).ToList();
}
