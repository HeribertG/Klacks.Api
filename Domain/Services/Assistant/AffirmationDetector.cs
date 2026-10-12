// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Precision-biased detector that decides whether a user chat message is a short affirmation /
/// go-ahead ("ja", "ok", "mach", "weiter", "yes", "do it") that confirms a previously requested
/// action. Combined with an outstanding pending confirmation, the orchestrator uses it to force a
/// real tool call so the model actually executes confirm_pending_action instead of replying in prose.
/// It deliberately favours precision over recall: a negation token anywhere ("nein", "nicht",
/// "abbrechen", "no", "cancel") or a question mark anywhere - not only a trailing one, see
/// RecipeReplyGuard.QuestionMarks for the marks covered - suppresses the signal, so an ambiguous reply
/// like "ja, aber nicht heute" or "ja? was kostet das" leaves the existing "auto" behaviour intact.
/// LeadsWithAffirmation additionally requires the reply to OPEN with the affirmation ("Ja, übernimm die Gruppen
/// so"), so a request that merely contains a courtesy word ("Neuen Mitarbeiter, bitte") is not read as accepting
/// an offer; "bitte"/"please" never count as the opening word.
/// IsExplicitConfirmation is the stricter server-side "real yes" that releases a held action: it opens with a pure
/// agreement, or is a short reply made only of reply vocabulary, and carries no conditional marker.
/// Core languages (de/en/fr/it) are handled by the word lists of AffirmationVocabulary. Plugin language entries are
/// loaded at startup via Configure() from conversation-signals.json files in each language plugin.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class AffirmationDetector
{
    private const int ShortReplyMaxWords = 3;
    private const int ShortReplyMaxNonSegmentedChars = 6;
    private const string ApostropheAscii = "'";
    private const string ApostropheTypographic = "’";

    private static readonly Regex WordPattern = new(@"\p{L}+(?:['’]\p{L}+)*", RegexOptions.Compiled);

    private static readonly object _configureLock = new();
    private static string[] _pluginAffirmationEntries = [];
    private static string[] _pluginNegationEntries = [];
    private static HashSet<string> _pluginVocabularyWords = new(StringComparer.Ordinal);

    /// <summary>
    /// Extends detection with plugin language entries. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> affirmations, IEnumerable<string> negations)
    {
        lock (_configureLock)
        {
            _pluginAffirmationEntries = PluginPhraseMatcher.Merge(_pluginAffirmationEntries, affirmations);
            _pluginNegationEntries = PluginPhraseMatcher.Merge(_pluginNegationEntries, negations);
            _pluginVocabularyWords = BuildVocabularyWords(_pluginAffirmationEntries);
        }
    }

    /// <summary>
    /// Discards every entry Configure ever merged in and restores the core-only state the detector starts
    /// in. Test-only: Configure writes process-wide static state additively, so without a way back a
    /// fixture that loads a language pack would decide the outcome of every fixture running after it.
    /// </summary>
    internal static void Reset()
    {
        lock (_configureLock)
        {
            _pluginAffirmationEntries = [];
            _pluginNegationEntries = [];
            _pluginVocabularyWords = new HashSet<string>(StringComparer.Ordinal);
        }
    }

    public static bool IsAffirmation(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.IndexOfAny(RecipeReplyGuard.QuestionMarks) >= 0)
        {
            return false;
        }

        var lower = message.ToLowerInvariant();
        var tokens = TokenValues(WordPattern.Matches(lower));

        if (tokens.Count == 0
            || tokens.Any(AffirmationVocabulary.NegationWords.Contains)
            || PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginNegationEntries))
        {
            return false;
        }

        return tokens.Any(AffirmationVocabulary.AffirmationWords.Contains)
            || PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginAffirmationEntries);
    }

    /// <summary>
    /// The server-side "real yes" behind a held action: the message is an affirmation (IsAffirmation), carries no
    /// conditional marker ("aber", "but", "nur", "only", "zuerst", "except" ...) and EITHER opens with a pure agreement
    /// once leading courtesy and filler words ("bitte", "ähm") are skipped, OR is a short reply made only of reply
    /// vocabulary. A pure agreement is a word like "ja" or "yes" - a plugin entry counts only when punctuation or the
    /// end of the message follows it directly, because pack entries include imperatives and homographs ("kör", "los",
    /// "si"). Go-ahead imperatives ("mach", "go", "weiter") never open a confirmation: "Mach einen Dienst für Montag"
    /// is a request. They count only in a short reply of at most ShortReplyMaxWords words (a non-segmented script:
    /// ShortReplyMaxNonSegmentedChars characters) in which every word is reply vocabulary ("mach das", "do it").
    /// Also decides which reply the chat entry points force into confirm_pending_action, so a reply the server would
    /// refuse is never forced.
    /// </summary>
    /// <param name="message">The raw user message that started the turn.</param>
    public static bool IsExplicitConfirmation(string? message)
    {
        if (!IsAffirmation(message))
        {
            return false;
        }

        var lower = message!.ToLowerInvariant();
        var words = WordPattern.Matches(lower).ToList();
        if (words.Any(word => AffirmationVocabulary.ConditionalWords.Contains(Normalize(word))))
        {
            return false;
        }

        var opener = words.FirstOrDefault(word => !IsSkippedBeforeOpener(Normalize(word)));
        if (opener == null)
        {
            return false;
        }

        return OpensWithPureAgreement(lower, opener) || IsShortReply(lower, words);
    }

    /// <summary>
    /// True when the message is an affirmation (IsAffirmation) AND opens with it: the first word is an affirmation
    /// token (courtesy words excluded) or the message starts, on a word boundary, with a plugin affirmation entry (also
    /// covers scripts without word separators such as "はい" or "是的").
    /// </summary>
    /// <param name="message">The raw user message that started the turn.</param>
    public static bool LeadsWithAffirmation(string? message)
    {
        if (!IsAffirmation(message))
        {
            return false;
        }

        var lower = message!.ToLowerInvariant();
        var first = WordPattern.Match(lower);
        if (!first.Success)
        {
            return false;
        }

        var word = Normalize(first);
        if (AffirmationVocabulary.AffirmationWords.Contains(word) && !AffirmationVocabulary.CourtesyWords.Contains(word))
        {
            return true;
        }

        return OpensWithPluginEntry(lower, first, requireClauseBoundary: false);
    }

    private static bool OpensWithPureAgreement(string lower, Match opener)
    {
        var word = Normalize(opener);
        if (AffirmationVocabulary.ReplyWords.Contains(word))
        {
            return true;
        }

        if (AffirmationVocabulary.AmbiguousReplyWords.Contains(word)
            && IsClauseBoundary(lower, opener.Index + opener.Length))
        {
            return true;
        }

        return OpensWithPluginEntry(lower, opener, requireClauseBoundary: true);
    }

    private static bool OpensWithPluginEntry(string lower, Match opener, bool requireClauseBoundary)
    {
        var opening = lower[opener.Index..];
        foreach (var entry in _pluginAffirmationEntries)
        {
            if (entry.Length == 0 || !opening.StartsWith(entry, StringComparison.Ordinal))
            {
                continue;
            }

            var end = opener.Index + entry.Length;
            if (!PluginPhraseMatcher.UsesNonSegmentedScript(entry) && end < lower.Length && char.IsLetter(lower[end]))
            {
                continue;
            }

            if (!requireClauseBoundary || IsClauseBoundary(lower, end))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsShortReply(string lower, IReadOnlyList<Match> words)
    {
        if (PluginPhraseMatcher.UsesNonSegmentedScript(lower))
        {
            return IsShortNonSegmentedReply(lower);
        }

        if (words.Count > ShortReplyMaxWords)
        {
            return false;
        }

        var tokens = TokenValues(words);
        var answersWithVocabulary = tokens.Any(token =>
                AffirmationVocabulary.ReplyWords.Contains(token) || AffirmationVocabulary.GoAheadWords.Contains(token))
            || PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginAffirmationEntries);

        return answersWithVocabulary && tokens.All(IsReplyVocabulary);
    }

    private static bool IsShortNonSegmentedReply(string lower)
    {
        var compact = new string(lower.Where(char.IsLetterOrDigit).ToArray());
        if (compact.Length == 0 || compact.Length > ShortReplyMaxNonSegmentedChars)
        {
            return false;
        }

        var remainder = compact;
        foreach (var entry in _pluginAffirmationEntries
                     .Where(PluginPhraseMatcher.UsesNonSegmentedScript)
                     .Select(entry => entry.Replace(" ", string.Empty))
                     .OrderByDescending(entry => entry.Length))
        {
            remainder = remainder.Replace(entry, string.Empty, StringComparison.Ordinal);
        }

        return remainder.Length == 0;
    }

    private static bool IsReplyVocabulary(string token)
    {
        return AffirmationVocabulary.AffirmationWords.Contains(token)
            || AffirmationVocabulary.FillerWords.Contains(token)
            || AffirmationVocabulary.CompanionWords.Contains(token)
            || _pluginVocabularyWords.Contains(token);
    }

    private static bool IsSkippedBeforeOpener(string token)
    {
        return AffirmationVocabulary.CourtesyWords.Contains(token) || AffirmationVocabulary.FillerWords.Contains(token);
    }

    private static bool IsClauseBoundary(string lower, int index)
    {
        return index >= lower.Length || char.IsPunctuation(lower[index]);
    }

    private static HashSet<string> BuildVocabularyWords(IEnumerable<string> entries)
    {
        return entries
            .SelectMany(entry => WordPattern.Matches(entry).Select(Normalize))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<string> TokenValues(IEnumerable<Match> matches)
    {
        return matches.Select(Normalize).ToList();
    }

    private static string Normalize(Match match)
    {
        return match.Value
            .Replace(ApostropheAscii, string.Empty, StringComparison.Ordinal)
            .Replace(ApostropheTypographic, string.Empty, StringComparison.Ordinal);
    }
}
