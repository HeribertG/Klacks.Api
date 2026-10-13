// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Immutable table of the words that one language lists as a yes and another as a no, generated from the
/// conversation-signals of all languages instead of maintained by hand. The detectors merge every language into one
/// global vocabulary, so such a word would silently mean "no" everywhere (negation wins); this table lets them settle
/// it by the UI language instead. Build compares the affirmations of every language with the negating lists
/// (negations, declines, cancellations, corrections) of every language; the same word on the same side in two
/// languages is not a collision. Match finds the collision words in a message and sorts them by how a UI language
/// resolves them; Describe renders the table for the startup log.
/// A collision word is Neutral for a language that lists it in no entry of its own but uses it as a token inside its own
/// multi-word user-reply phrases (Danish "nej tak"): there the bare word is no signal and the rest of the message
/// decides. Space-segmented words only; the assistant-side gap indicators are not user vocabulary and do not count.
/// The core languages (de/en/fr/it) contribute their negations only, because the core affirmation vocabulary
/// (AffirmationVocabulary) carries no language: a collision between a core affirmation and a pack negation is NOT found
/// by Build, and only the guard test NoCoreAffirmationWord_IsAnyLanguagesNegation_AndNoCoreNegation_IsAnyPacksAffirmation
/// keeps the shipped vocabulary free of one.
/// </summary>
/// <param name="entries">The collisions, keyed by normalized word</param>

using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed class ConversationSignalCollisionTable
{
    private const string DescribeHeader = "Conversation-signal collisions: ";
    private const string DescribeLineTemplate = "{0}: yes in [{1}], no in [{2}] ({3})";
    private const string DescribeNeutralTemplate = ", neutral in [{0}]";
    private const string ListSeparator = ", ";
    private const char PhraseWordSeparator = ' ';

    private static readonly Regex PhraseTokenPattern =
        new(@"[\p{L}\p{M}]+(?:['’][\p{L}\p{M}]+)*", RegexOptions.Compiled);

    public static readonly ConversationSignalCollisionTable Empty =
        new(new Dictionary<string, ConversationSignalCollisionEntry>());

    private readonly FrozenDictionary<string, ConversationSignalCollisionEntry> _entries;
    private readonly ConversationSignalCollisionEntry[] _ordered;

    private ConversationSignalCollisionTable(IDictionary<string, ConversationSignalCollisionEntry> entries)
    {
        _entries = entries.ToFrozenDictionary(StringComparer.Ordinal);
        _ordered = entries.Values.OrderBy(entry => entry.Word, StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyDictionary<string, ConversationSignalCollisionEntry> Entries => _entries;

    public bool IsEmpty => _entries.Count == 0;

    public static string Key(string? word) =>
        (word ?? string.Empty).Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();

    public static ConversationSignalCollisionTable Build(IEnumerable<ConversationLanguageSignals> signals)
    {
        var languages = signals.ToList();
        var affirming = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var negating = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var categories = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var language in languages)
        {
            Collect(affirming, language.Affirmations, language.Language, null, categories);
            Collect(negating, language.Negations, language.Language, ConversationSignalCategories.Negations, categories);
            Collect(negating, language.Declines, language.Language, ConversationSignalCategories.Declines, categories);
            Collect(negating, language.Cancellations, language.Language, ConversationSignalCategories.Cancellations, categories);
            Collect(negating, language.Corrections, language.Language, ConversationSignalCategories.Corrections, categories);
        }

        var collisions = new Dictionary<string, ConversationSignalCollisionEntry>(StringComparer.Ordinal);
        foreach (var word in affirming.Keys.Where(negating.ContainsKey).OrderBy(word => word, StringComparer.Ordinal))
        {
            var neutral = NeutralLanguagesOf(word, languages)
                .Except(affirming[word], StringComparer.OrdinalIgnoreCase)
                .Except(negating[word], StringComparer.OrdinalIgnoreCase)
                .OrderBy(language => language, StringComparer.OrdinalIgnoreCase)
                .ToList();

            collisions[word] = new ConversationSignalCollisionEntry(
                word,
                affirming[word].ToList(),
                negating[word].ToList(),
                categories[word].ToList(),
                neutral);
        }

        return collisions.Count == 0 ? Empty : new ConversationSignalCollisionTable(collisions);
    }

    public ConversationSignalCollisionMatch Match(
        string lowerMessage, IReadOnlyCollection<string> tokens, string? language)
    {
        if (_entries.Count == 0)
        {
            return ConversationSignalCollisionMatch.None;
        }

        HashSet<string>? suppressedNegations = null;
        HashSet<string>? suppressedAffirmations = null;
        List<ConversationSignalCollisionEntry>? unresolved = null;
        List<ConversationSignalCollisionEntry>? neutral = null;

        foreach (var entry in _ordered)
        {
            if (!Occurs(entry.Word, lowerMessage, tokens))
            {
                continue;
            }

            switch (entry.ResolveFor(language))
            {
                case CollisionResolution.Affirming:
                    (suppressedNegations ??= new HashSet<string>(StringComparer.Ordinal)).Add(entry.Word);
                    break;
                case CollisionResolution.Negating:
                    (suppressedAffirmations ??= new HashSet<string>(StringComparer.Ordinal)).Add(entry.Word);
                    break;
                case CollisionResolution.Neutral:
                    (neutral ??= []).Add(entry);
                    break;
                default:
                    (unresolved ??= []).Add(entry);
                    break;
            }
        }

        if (suppressedNegations == null && suppressedAffirmations == null && unresolved == null && neutral == null)
        {
            return ConversationSignalCollisionMatch.None;
        }

        return new ConversationSignalCollisionMatch(
            suppressedNegations ?? [], suppressedAffirmations ?? [], unresolved ?? [], neutral ?? []);
    }

    public string Describe()
    {
        var text = new StringBuilder(DescribeHeader).Append(_entries.Count);
        foreach (var entry in _ordered)
        {
            text.AppendLine().Append("  ").AppendFormat(
                CultureInfo.InvariantCulture,
                DescribeLineTemplate,
                entry.Word,
                string.Join(ListSeparator, entry.AffirmingLanguages),
                string.Join(ListSeparator, entry.NegatingLanguages),
                string.Join(ListSeparator, entry.NegatingCategories));

            if (entry.NeutralLanguages.Count > 0)
            {
                text.AppendFormat(
                    CultureInfo.InvariantCulture,
                    DescribeNeutralTemplate,
                    string.Join(ListSeparator, entry.NeutralLanguages));
            }
        }

        return text.ToString();
    }

    private static bool Occurs(string word, string lowerMessage, IReadOnlyCollection<string> tokens)
    {
        if (tokens.Contains(word))
        {
            return true;
        }

        var matchesAsSubstring = word.Contains(' ') || PluginPhraseMatcher.UsesNonSegmentedScript(word);
        return matchesAsSubstring && lowerMessage.Contains(word, StringComparison.Ordinal);
    }

    private static IEnumerable<string> NeutralLanguagesOf(string word, List<ConversationLanguageSignals> languages)
    {
        if (PluginPhraseMatcher.UsesNonSegmentedScript(word))
        {
            return [];
        }

        var wordTokens = PhraseTokens(word);
        if (wordTokens.Count == 0)
        {
            return [];
        }

        return languages
            .Where(language => language.Affirmations
                .Concat(language.Negations).Concat(language.Declines)
                .Concat(language.Cancellations).Concat(language.Corrections)
                .Any(entry => ContainsInsideLongerPhrase(PhraseTokens(entry), wordTokens)))
            .Select(language => language.Language)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> PhraseTokens(string entry) =>
        PhraseTokenPattern.Matches(Key(entry)).Select(match => match.Value).ToList();

    private static bool ContainsInsideLongerPhrase(List<string> phrase, List<string> word)
    {
        if (phrase.Count <= word.Count)
        {
            return false;
        }

        var padded = PhraseWordSeparator + string.Join(PhraseWordSeparator, phrase) + PhraseWordSeparator;
        var needle = PhraseWordSeparator + string.Join(PhraseWordSeparator, word) + PhraseWordSeparator;
        return padded.Contains(needle, StringComparison.Ordinal);
    }

    private static void Collect(
        Dictionary<string, SortedSet<string>> languagesByWord,
        IEnumerable<string> words,
        string language,
        string? category,
        Dictionary<string, SortedSet<string>> categoriesByWord)
    {
        foreach (var word in words)
        {
            var key = Key(word);
            if (key.Length == 0)
            {
                continue;
            }

            if (!languagesByWord.TryGetValue(key, out var languages))
            {
                languages = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                languagesByWord[key] = languages;
            }

            languages.Add(language);

            if (category != null)
            {
                if (!categoriesByWord.TryGetValue(key, out var cats))
                {
                    cats = new SortedSet<string>(StringComparer.Ordinal);
                    categoriesByWord[key] = cats;
                }

                cats.Add(category);
            }
        }
    }
}
