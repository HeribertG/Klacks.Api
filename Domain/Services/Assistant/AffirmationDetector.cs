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
/// so"), so a request that merely contains a courtesy word ("Neuen Mitarbeiter, bitte") or opens with an imperative
/// or a homograph ("Los empleados nuevos, por favor") is not read as accepting an offer; "bitte"/"please" never count
/// as the opening word, and go-ahead words, "si" and pack entries open only as a short reply, before punctuation, or -
/// for a go-ahead word and a pack entry, which is how a spoken reply without commas arrives ("Sí hazlo por favor") -
/// when the next word is answer vocabulary too: a reply word, a word of the affirmation entries of the UI language's own
/// pack (not the merged vocabulary of all packs, which would let foreign words such as "to" make "Go to the schedule"
/// an acceptance) and, after a pack entry only, a go-ahead word. Courtesy words ("bitte", "please") in between are
/// skipped, never counted: "Mach bitte einen Dienst für Montag" is a request, "Mach bitte" an acceptance. "Los
/// empleados nuevos", "Si Pérez falta" and "Mach einen Dienst für Montag" stay requests: their next word answers nothing.
/// IsExplicitConfirmation is the stricter server-side "real yes" that releases a held action: it opens with a pure
/// agreement, or is a short reply made only of reply vocabulary, and carries no conditional marker.
/// Core languages (de/en/fr/it) are handled by the word lists of AffirmationVocabulary. Plugin language entries are
/// loaded at startup via Configure() from conversation-signals.json files in each language plugin and merged into one
/// global vocabulary, which is why a word that is a yes in one language and a no in another (Polish "tak" against
/// Malay "tak") is settled by the UI language through ConversationSignalCollisions: a language that lists the word as
/// a yes reads it as a yes, one that lists it as a no reads it as a no, and a language that only knows it inside its own
/// phrases (Danish "nej tak") reads it as no signal at all, so the rest of the message decides ("Ja tak" is a yes, "Nej
/// tak" a no). A message made of nothing but such a neutral word ("tak") is Ambiguous, never a refusal: a bare "tak" may
/// be thanks or an acceptance, and an Ambiguous reply keeps a pending gate open where NotConfirmed would abort it. Any
/// other language leaves the word Ambiguous and the reply is neither a confirmation nor a refusal until the user says it
/// unambiguously. The message is composed (NFC) before it is read, like the entries, so a decomposed reply still matches.
/// </summary>
/// <param name="message">The raw user message that started the turn.</param>
/// <param name="language">UI language of the turn, used only to settle words the languages disagree about; null when unknown.</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class AffirmationDetector
{
    private const int ShortReplyMaxWords = 3;
    private const int ShortReplyMaxNonSegmentedChars = 6;
    private const string ApostropheAscii = "'";
    private const string ApostropheTypographic = "’";
    private const char NeutralWordSeparator = ' ';

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
            _pluginAffirmationEntries = PluginPhraseMatcher.Merge(
                _pluginAffirmationEntries, affirmations.Select(PluginPhraseMatcher.ComposeNfc));
            _pluginNegationEntries = PluginPhraseMatcher.Merge(
                _pluginNegationEntries, negations.Select(PluginPhraseMatcher.ComposeNfc));
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

    public static bool IsAffirmation(string? message, string? language = null)
    {
        return AssessAffirmation(message, language).IsConfirmed;
    }

    /// <summary>
    /// IsAffirmation with the third state: Ambiguous when the message carries a word that one language lists as a yes
    /// and another as a no, and the UI language does not settle it. A caller that must not guess (a recipe gate that
    /// would otherwise be aborted as declined) tells Ambiguous apart from NotConfirmed.
    /// </summary>
    /// <param name="message">The raw user message that started the turn.</param>
    /// <param name="language">UI language of the turn; null when unknown, which leaves a colliding word Ambiguous.</param>
    public static ConfirmationAssessment AssessAffirmation(string? message, string? language = null)
    {
        return Read(message, language, out _);
    }

    public static bool IsExplicitConfirmation(string? message, string? language = null)
    {
        return AssessExplicitConfirmation(message, language).IsConfirmed;
    }

    /// <summary>
    /// The server-side "real yes" behind a held action: the message is an affirmation (IsAffirmation), carries no
    /// conditional marker ("aber", "but", "nur", "only", "zuerst", "except" ...) and EITHER opens with a pure agreement
    /// once leading courtesy and filler words ("bitte", "ähm") are skipped, OR is a short reply made only of reply
    /// vocabulary. A pure agreement is a word like "ja" or "yes" - a plugin entry counts only when punctuation, the
    /// end of the message or a further answer word (reply, go-ahead, courtesy or UI-language pack vocabulary: "Sí hazlo
    /// por favor") follows it directly, because pack entries include imperatives and homographs ("kör", "los", "si"). Go-ahead imperatives ("mach", "go", "weiter") never open a confirmation: "Mach einen Dienst für Montag"
    /// is a request. They count only in a short reply of at most ShortReplyMaxWords words (a non-segmented script:
    /// ShortReplyMaxNonSegmentedChars characters) in which every word is reply vocabulary ("mach das", "do it").
    /// Also decides which reply the chat entry points force into confirm_pending_action, so a reply the server would
    /// refuse is never forced. The verdict is Ambiguous, never Confirmed, when the reply carries a word that
    /// languages disagree about (yes in one, no in another) and the UI language does not settle it.
    /// </summary>
    /// <param name="message">The raw user message that started the turn.</param>
    /// <param name="language">UI language of the turn; null when unknown, which leaves a colliding word Ambiguous.</param>
    public static ConfirmationAssessment AssessExplicitConfirmation(string? message, string? language = null)
    {
        var assessment = Read(message, language, out var reading);
        if (!assessment.IsConfirmed || reading == null)
        {
            return assessment;
        }

        if (reading.Words.Any(word => AffirmationVocabulary.ConditionalWords.Contains(Normalize(word))))
        {
            return ConfirmationAssessment.NotConfirmed;
        }

        var opener = reading.Words.FirstOrDefault(word => !IsSkippedBeforeOpener(Normalize(word)));
        if (opener == null)
        {
            return ConfirmationAssessment.NotConfirmed;
        }

        return OpensWithPureAgreement(reading, opener) || IsShortReply(reading)
            ? ConfirmationAssessment.Confirmed
            : ConfirmationAssessment.NotConfirmed;
    }

    /// <summary>
    /// True when the message is an affirmation (IsAffirmation) AND opens with it: the first word is a pure agreement
    /// (courtesy words excluded), or the message starts, before punctuation or the end, with a go-ahead word, "si" or a
    /// plugin affirmation entry (also covers scripts without word separators such as "はい" or "是的", where a blank
    /// after the entry ends the clause just like punctuation - "ใช่ ใช้กลุ่มตามนี้เลย"), or the whole
    /// message is a short reply made of reply vocabulary. A go-ahead word or pack entry that merely opens a longer
    /// sentence does not count - "Los empleados nuevos, por favor" and "Mach einen Dienst für Montag" are requests, so
    /// the recipe fallback must stay open for them - unless the next word is answer vocabulary as well (see the class
    /// summary): "Sí hazlo por favor" is an acceptance without a single comma.
    /// </summary>
    /// <param name="message">The raw user message that started the turn.</param>
    /// <param name="language">UI language of the turn; null when unknown.</param>
    public static bool LeadsWithAffirmation(string? message, string? language = null)
    {
        if (!Read(message, language, out var reading).IsConfirmed || reading == null)
        {
            return false;
        }

        var first = reading.Words[0];
        var word = Normalize(first);
        if (AffirmationVocabulary.ReplyWords.Contains(word))
        {
            return true;
        }

        if (AffirmationVocabulary.AmbiguousReplyWords.Contains(word)
            && IsClauseBoundary(reading.Lower, first.Index + first.Length))
        {
            return true;
        }

        if (AffirmationVocabulary.GoAheadWords.Contains(word)
            && IsFollowedByAnswerWord(reading, first.Index + first.Length, openerIsGoAhead: true))
        {
            return true;
        }

        return OpensWithPluginEntry(reading, first, requireClauseBoundary: true, spaceEndsNonSegmentedEntry: true)
            || IsShortReply(reading);
    }

    private static ConfirmationAssessment Read(string? message, string? language, out Reading? reading)
    {
        reading = null;
        if (string.IsNullOrWhiteSpace(message) || message.IndexOfAny(RecipeReplyGuard.QuestionMarks) >= 0)
        {
            return ConfirmationAssessment.NotConfirmed;
        }

        var lower = PluginPhraseMatcher.ComposeNfc(message).ToLowerInvariant();
        var words = WordPattern.Matches(lower).ToList();
        var tokens = TokenValues(words);
        if (tokens.Count == 0)
        {
            return ConfirmationAssessment.NotConfirmed;
        }

        var collisions = ConversationSignalCollisions.Current.Match(lower, tokens, language);
        var affirmationEntries = collisions.ExcludeFromAffirmations(_pluginAffirmationEntries, includeUnresolved: true);
        var negationEntries = collisions.ExcludeFromNegations(_pluginNegationEntries, includeUnresolved: true);
        var vocabularyWords = ReferenceEquals(affirmationEntries, _pluginAffirmationEntries)
            ? _pluginVocabularyWords
            : BuildVocabularyWords(affirmationEntries);

        reading = new Reading(lower, words, tokens, affirmationEntries, vocabularyWords, language);

        if (tokens.Any(AffirmationVocabulary.NegationWords.Contains)
            || PluginPhraseMatcher.MatchesAny(lower, tokens, negationEntries))
        {
            return ConfirmationAssessment.NotConfirmed;
        }

        if (collisions.Unresolved.Count > 0)
        {
            var unresolved = collisions.Unresolved[0];
            return new ConfirmationAssessment(ConfirmationVerdict.Ambiguous, unresolved.Word, unresolved.AllLanguages);
        }

        if (IsOnlyNeutralWords(tokens, collisions))
        {
            var neutral = collisions.Neutral[0];
            return new ConfirmationAssessment(ConfirmationVerdict.Ambiguous, neutral.Word, neutral.AllLanguages);
        }

        return tokens.Any(AffirmationVocabulary.AffirmationWords.Contains)
            || PluginPhraseMatcher.MatchesAny(lower, tokens, affirmationEntries)
                ? ConfirmationAssessment.Confirmed
                : ConfirmationAssessment.NotConfirmed;
    }

    private static bool IsOnlyNeutralWords(List<string> tokens, ConversationSignalCollisionMatch collisions)
    {
        if (collisions.Neutral.Count == 0)
        {
            return false;
        }

        var neutralWords = collisions.Neutral
            .SelectMany(entry => entry.Word.Split(NeutralWordSeparator, StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        return tokens.All(token =>
            neutralWords.Contains(token) || IsSkippedBeforeOpener(token));
    }

    private static bool OpensWithPureAgreement(Reading reading, Match opener)
    {
        var word = Normalize(opener);
        if (AffirmationVocabulary.ReplyWords.Contains(word))
        {
            return true;
        }

        if (AffirmationVocabulary.AmbiguousReplyWords.Contains(word)
            && IsClauseBoundary(reading.Lower, opener.Index + opener.Length))
        {
            return true;
        }

        return OpensWithPluginEntry(reading, opener, requireClauseBoundary: true);
    }

    private static bool OpensWithPluginEntry(
        Reading reading, Match opener, bool requireClauseBoundary, bool spaceEndsNonSegmentedEntry = false)
    {
        var lower = reading.Lower;
        var opening = lower[opener.Index..];
        foreach (var entry in reading.AffirmationEntries)
        {
            if (!PluginPhraseMatcher.StartsWithEntryAtWordEnd(opening, entry))
            {
                continue;
            }

            var end = opener.Index + entry.Length;

            var endsOnBlank = spaceEndsNonSegmentedEntry
                && PluginPhraseMatcher.UsesNonSegmentedScript(entry)
                && end < lower.Length
                && char.IsWhiteSpace(lower[end]);

            if (!requireClauseBoundary
                || IsClauseBoundary(lower, end)
                || endsOnBlank
                || IsFollowedByAnswerWord(reading, end, openerIsGoAhead: false))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFollowedByAnswerWord(Reading reading, int index, bool openerIsGoAhead)
    {
        var skippedCourtesy = false;
        var position = index;
        foreach (var word in reading.Words.Where(word => word.Index >= index))
        {
            if (!string.IsNullOrWhiteSpace(reading.Lower[position..word.Index]))
            {
                return false;
            }

            var token = Normalize(word);
            if (AffirmationVocabulary.CourtesyWords.Contains(token))
            {
                skippedCourtesy = true;
                position = word.Index + word.Length;
                continue;
            }

            return AffirmationVocabulary.ReplyWords.Contains(token)
                || (!openerIsGoAhead && AffirmationVocabulary.GoAheadWords.Contains(token))
                || IsWordOfTheUiLanguagePack(token, reading.Language);
        }

        return skippedCourtesy;
    }

    private static bool IsWordOfTheUiLanguagePack(string token, string? language)
    {
        var entries = ConversationSignalCollisions.AffirmationsOf(language);
        return entries.Count > 0
            && BuildVocabularyWords(entries.Select(entry => PluginPhraseMatcher.ComposeNfc(entry).ToLowerInvariant()))
                .Contains(token);
    }

    private static bool IsShortReply(Reading reading)
    {
        var lower = reading.Lower;
        if (PluginPhraseMatcher.UsesNonSegmentedScript(lower))
        {
            return IsShortNonSegmentedReply(reading);
        }

        if (reading.Words.Count > ShortReplyMaxWords)
        {
            return false;
        }

        var tokens = reading.Tokens;
        var answersWithVocabulary = tokens.Any(token =>
                AffirmationVocabulary.ReplyWords.Contains(token) || AffirmationVocabulary.GoAheadWords.Contains(token))
            || PluginPhraseMatcher.MatchesAny(lower, tokens, reading.AffirmationEntries);

        return answersWithVocabulary && tokens.All(token => IsReplyVocabulary(token, reading.VocabularyWords));
    }

    private static bool IsShortNonSegmentedReply(Reading reading)
    {
        var compact = new string(reading.Lower.Where(char.IsLetterOrDigit).ToArray());
        if (compact.Length == 0 || compact.Length > ShortReplyMaxNonSegmentedChars)
        {
            return false;
        }

        var remainder = compact;
        foreach (var entry in reading.AffirmationEntries
                     .Where(PluginPhraseMatcher.UsesNonSegmentedScript)
                     .Select(entry => entry.Replace(" ", string.Empty))
                     .OrderByDescending(entry => entry.Length))
        {
            remainder = remainder.Replace(entry, string.Empty, StringComparison.Ordinal);
        }

        return remainder.Length == 0;
    }

    private static bool IsReplyVocabulary(string token, HashSet<string> pluginVocabularyWords)
    {
        return AffirmationVocabulary.AffirmationWords.Contains(token)
            || AffirmationVocabulary.FillerWords.Contains(token)
            || AffirmationVocabulary.CompanionWords.Contains(token)
            || pluginVocabularyWords.Contains(token);
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

    private sealed record Reading(
        string Lower,
        List<Match> Words,
        List<string> Tokens,
        string[] AffirmationEntries,
        HashSet<string> VocabularyWords,
        string? Language);
}
