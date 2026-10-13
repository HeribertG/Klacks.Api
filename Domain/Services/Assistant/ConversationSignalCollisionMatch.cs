// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The collision words found in one message, sorted by how the UI language resolves them: SuppressedNegations are
/// words the language reads as a yes (a detector must not treat them as a no), SuppressedAffirmations are words the
/// language reads as a no, Neutral are words the language uses only inside its own phrases (no yes and no signal of
/// their own, the rest of the message decides), Unresolved are words the language cannot settle. The filter methods
/// drop those words from a detector's merged entry list; the list is returned untouched, without allocation, when
/// nothing matched.
/// </summary>
/// <param name="suppressedNegations">Keys of words the UI language reads as affirmations</param>
/// <param name="suppressedAffirmations">Keys of words the UI language reads as negations</param>
/// <param name="unresolved">Entries the UI language does not settle, or settles twice</param>
/// <param name="neutral">Entries the UI language knows only inside its own phrases</param>

namespace Klacks.Api.Domain.Services.Assistant;

public sealed class ConversationSignalCollisionMatch
{
    public static readonly ConversationSignalCollisionMatch None = new([], [], [], []);

    private readonly HashSet<string> _unresolvedKeys;
    private readonly HashSet<string> _neutralKeys;

    public ConversationSignalCollisionMatch(
        HashSet<string> suppressedNegations,
        HashSet<string> suppressedAffirmations,
        IReadOnlyList<ConversationSignalCollisionEntry> unresolved,
        IReadOnlyList<ConversationSignalCollisionEntry> neutral)
    {
        SuppressedNegations = suppressedNegations;
        SuppressedAffirmations = suppressedAffirmations;
        Unresolved = unresolved;
        Neutral = neutral;
        _unresolvedKeys = unresolved.Select(entry => entry.Word).ToHashSet(StringComparer.Ordinal);
        _neutralKeys = neutral.Select(entry => entry.Word).ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlySet<string> SuppressedNegations { get; }

    public IReadOnlySet<string> SuppressedAffirmations { get; }

    public IReadOnlyList<ConversationSignalCollisionEntry> Unresolved { get; }

    public IReadOnlyList<ConversationSignalCollisionEntry> Neutral { get; }

    public bool IsEmpty =>
        SuppressedNegations.Count == 0 && SuppressedAffirmations.Count == 0 && Unresolved.Count == 0 && Neutral.Count == 0;

    public string[] ExcludeFromNegations(string[] entries, bool includeUnresolved) =>
        Filter(entries, SuppressedNegations, includeUnresolved);

    public string[] ExcludeFromAffirmations(string[] entries, bool includeUnresolved) =>
        Filter(entries, SuppressedAffirmations, includeUnresolved);

    private string[] Filter(string[] entries, IReadOnlySet<string> suppressed, bool includeUnresolved)
    {
        var dropUnresolved = includeUnresolved && _unresolvedKeys.Count > 0;
        if (suppressed.Count == 0 && !dropUnresolved && _neutralKeys.Count == 0)
        {
            return entries;
        }

        return entries
            .Where(entry =>
            {
                var key = ConversationSignalCollisionTable.Key(entry);
                return !suppressed.Contains(key)
                    && !_neutralKeys.Contains(key)
                    && !(dropUnresolved && _unresolvedKeys.Contains(key));
            })
            .ToArray();
    }
}
