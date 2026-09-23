// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Code-side guard rails for a clarification question before it is sent to an employee. The LLM is told
/// the same rules, this guard enforces them: not empty, at most MaxQuestionLength characters, at most
/// MaxQuestionSentences sentences, ending with the question mark of the language, no link, and no health
/// term of any language from ClarificationHealthTerms. A period only ends a sentence before whitespace or
/// the end and never after a digit or a single letter, so a time (14.00), a date (24.09.) or an
/// abbreviation (z. B.) does not count. The question must END with ?, the full-width ？ or the Arabic ؟;
/// the Greek question mark (; or U+037E) only counts when the text contains Greek letters. For the
/// health-term check only, every whole word of the system-inserted context (shift, station or ward names
/// such as "Frühdienst Chirurgie" or "Spital Nord") that would itself trigger a health term is removed
/// from the question first (other context words such as "de" or "di" stay, so a multi-word term like
/// "mal de tête" cannot be split apart by a shift name like "Service de nuit"), and the fixed
/// sick-leave phrasings of ClarificationHealthTerms.AllowedAbsencePhrases (arrêt maladie, in malattia,
/// krankheitsbedingt, sick leave, ...) are neutralised; stems of the compound languages match inside
/// compounds (Rückenschmerzen, Hausarzt, hoofdpijn, huvudvärk) and the match mode per language is defined
/// by ClarificationHealthTerms. Any violation means no question is sent; the message then stays on the
/// regular path.
/// </summary>
/// <param name="question">The composed question</param>
/// <param name="systemInsertedContext">Text the system itself put into the prompt (the affected shift with
/// its name, station and time); its words that contain a health term are ignored by the health-term check,
/// null when there is none. Only system-built text belongs here, never the employee's message or a draft</param>
/// <param name="violation">Why the question was rejected, empty when it passed</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Inbound;

public static class ClarificationQuestionGuard
{
    public const string EmptyViolation = "the question is empty";
    public const string TooLongViolation = "the question is longer than allowed";
    public const string TooManySentencesViolation = "the question has more sentences than allowed";
    public const string NotAQuestionViolation = "the text is not phrased as a question";
    public const string LinkViolation = "the question contains a link";
    public const string HealthTermViolationPrefix = "the question contains the health term: ";

    private const string RemovedTextReplacement = " ";

    private static readonly Regex SentenceTerminator = new(
        @"(?<!\d)(?<!(?:^|[^\p{L}])\p{L})\.(?=\s|$)|[!?;\u037E](?=\s|$)|[。！？؟]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Word = new(
        @"\p{L}+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex GreekLetter = new(
        @"(?=\p{L})[\u0370-\u03FF\u1F00-\u1FFF]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly char[] QuestionMarks = ['?', '？', '؟'];

    private static readonly char[] GreekQuestionMarks = [';', '\u037E'];

    private static readonly string[] LinkMarkers = ["://", "www."];

    public static bool IsAcceptable(string? question, out string violation)
        => IsAcceptable(question, null, out violation);

    public static bool IsAcceptable(string? question, string? systemInsertedContext, out string violation)
    {
        violation = string.Empty;
        if (string.IsNullOrWhiteSpace(question))
        {
            violation = EmptyViolation;
            return false;
        }

        var text = question.Trim();
        if (text.Length > InboundClarificationConstants.MaxQuestionLength)
        {
            violation = TooLongViolation;
            return false;
        }

        if (SentenceTerminator.Matches(text).Count > InboundClarificationConstants.MaxQuestionSentences)
        {
            violation = TooManySentencesViolation;
            return false;
        }

        if (!EndsWithQuestionMark(text))
        {
            violation = NotAQuestionViolation;
            return false;
        }

        var lowerText = text.ToLowerInvariant();
        if (LinkMarkers.Any(marker => lowerText.Contains(marker, StringComparison.Ordinal)))
        {
            violation = LinkViolation;
            return false;
        }

        var healthTerm = FindHealthTerm(RemoveContextWords(lowerText, systemInsertedContext));
        if (healthTerm != null)
        {
            violation = HealthTermViolationPrefix + healthTerm;
            return false;
        }

        return true;
    }

    public static string? FindHealthTerm(string lowerText)
    {
        var text = RemoveAllowedAbsencePhrases(lowerText);
        foreach (var (language, terms) in ClarificationHealthTerms.ByLanguage)
        {
            var substringMatch = ClarificationHealthTerms.SubstringMatchLanguages.Contains(language)
                || ClarificationHealthTerms.CompoundSubstringMatchLanguages.Contains(language);
            foreach (var term in terms)
            {
                var hit = substringMatch
                    ? text.Contains(term, StringComparison.Ordinal)
                    : OccursAtWordStart(text, term);
                if (hit)
                {
                    return term;
                }
            }
        }

        return null;
    }

    private static bool EndsWithQuestionMark(string text)
    {
        var last = text[^1];
        if (QuestionMarks.Contains(last))
        {
            return true;
        }

        return GreekQuestionMarks.Contains(last) && GreekLetter.IsMatch(text);
    }

    private static string RemoveContextWords(string lowerText, string? systemInsertedContext)
    {
        if (string.IsNullOrWhiteSpace(systemInsertedContext))
        {
            return lowerText;
        }

        var contextWords = Word.Matches(systemInsertedContext.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(word => FindHealthTerm(word) != null)
            .ToHashSet(StringComparer.Ordinal);

        if (contextWords.Count == 0)
        {
            return lowerText;
        }

        return Word.Replace(
            lowerText,
            match => contextWords.Contains(match.Value) ? RemovedTextReplacement : match.Value);
    }

    private static string RemoveAllowedAbsencePhrases(string lowerText)
    {
        var text = lowerText;
        foreach (var phrase in ClarificationHealthTerms.AllowedAbsencePhrases)
        {
            text = text.Replace(phrase, RemovedTextReplacement, StringComparison.Ordinal);
        }

        return text;
    }

    private static bool OccursAtWordStart(string text, string term)
    {
        var index = text.IndexOf(term, StringComparison.Ordinal);
        while (index >= 0)
        {
            if (index == 0 || !char.IsLetter(text[index - 1]))
            {
                return true;
            }

            index = text.IndexOf(term, index + 1, StringComparison.Ordinal);
        }

        return false;
    }
}
