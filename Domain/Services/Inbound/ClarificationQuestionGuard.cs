// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Code-side guard rails for a clarification question before it is sent to an employee. The LLM is told
/// the same rules, this guard enforces them: not empty, at most MaxQuestionLength characters, at most
/// MaxQuestionSentences sentences, ending with the question mark of the language, no link, no "@" and no
/// phone-number-like digit run (at least MinPhoneNumberDigits digits, with whitespace, dashes, dots,
/// colons, plus signs or slashes allowed between them). A run is exempted from the phone check when it is itself
/// made of one or more date or time tokens (day 1-31, month 1-12, hour 0-24, minute 0-59 are range-checked),
/// separated only by whitespace or a dash, never a bare dot between tokens: ISO dates (2026-09-23), dotted
/// dates with or without spaces after the dots (24.09., 24.09.2026, 23. 9. 2026, Hungarian 2026. 09. 23.),
/// slash dates (23/09/2026 or 09/23/2026), dashed dates (23-09-2026), a bare four-digit year (2026) and
/// times (14:00 or 14.00). That lets a shift date/time such as "2026-09-23 14:00-22:00", "vom 24.09. - 26.09.",
/// "23. 9. 2026 od 14:00" or "23 September 2026 14:00-22:00" through while still rejecting a dot-grouped
/// digit run such as "06.12.34.56.78". Residual risk: a phone number written as several valid day/month
/// pairs joined by ". " (for example "01. 02. 03. 04") is treated as dates. The date-or-time run pattern
/// has overlapping token alternatives and therefore runs on the non-backtracking regex engine, so a
/// crafted digit run cannot cause catastrophic backtracking. A period only ends a sentence before
/// whitespace or the end and never after a digit or a single letter, so a time (14.00), a date (24.09.)
/// or an abbreviation (z. B.) does not count.
/// The question must END with ?, the full-width ？ or the Arabic ؟;
/// the Greek question mark (; or U+037E) only counts when the text contains Greek letters. For the
/// health-term check only, every whole word of the system-inserted context (shift, station or ward names
/// such as "Frühdienst Chirurgie" or "Spital Nord") that would itself trigger a health term is removed
/// from the question first (other context words such as "de" or "di" stay, so a multi-word term like
/// "mal de tête" cannot be split apart by a shift name like "Service de nuit"), and the fixed
/// sick-leave phrasings of ClarificationHealthTerms.AllowedAbsencePhrases (arrêt maladie, in malattia,
/// krankheitsbedingt, sick leave, ...) and the harmless word parts of
/// ClarificationHealthTerms.HarmlessWordParts (unterbrechen, fevereiro, hospitality, ...) are neutralised;
/// stems of the compound languages match inside compounds (Rückenschmerzen, Hausarzt, hoofdpijn, huvudvärk),
/// the match mode per language is defined by ClarificationHealthTerms, and the short
/// ClarificationHealthTerms.WholeWordTerms (flu, pain, tos, dor, ból) only match as whole words. Question,
/// context and every text given to FindHealthTerm are normalised to Unicode NFC first, so a decomposed
/// umlaut or accent cannot slip past a term. Any violation means no question is sent; the message then
/// stays on the regular path.
/// </summary>
/// <param name="question">The composed question</param>
/// <param name="systemInsertedContext">Text the system itself put into the prompt (the affected shift with
/// its name, station and time); its words that contain a health term are ignored by the health-term check,
/// null when there is none. Only system-built text belongs here, never the employee's message or a draft</param>
/// <param name="violation">Why the question was rejected, empty when it passed</param>

using System.Text;
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
    public const string EmailAddressViolation = "the question contains an email address";
    public const string PhoneNumberViolation = "the question contains a phone number";
    public const string HealthTermViolationCategory = "the question contains a health term";
    public const string HealthTermViolationPrefix = "the question contains the health term: ";

    private const string RemovedTextReplacement = " ";
    private const char EmailAtSign = '@';
    private const int MinPhoneNumberDigits = 7;

    private static readonly Regex PhoneNumberDigitRun = new(
        @"\d[\d\s\-.:+/]*\d",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string DayPattern = @"(?:0?[1-9]|[12]\d|3[01])";
    private const string MonthPattern = @"(?:0?[1-9]|1[0-2])";
    private const string FourDigitYearPattern = @"(?:19|20)\d{2}";
    private const string AnyYearPattern = @"(?:19|20)?\d{2}";
    private const string HourPattern = @"(?:[01]?\d|2[0-4])";
    private const string MinutePattern = @"[0-5]\d";

    private static readonly string[] DateOrTimeTokens =
    [
        $@"{FourDigitYearPattern}-{MonthPattern}-{DayPattern}",
        $@"{FourDigitYearPattern}\.\s?{MonthPattern}\.\s?{DayPattern}\.?",
        $@"{DayPattern}\.{MonthPattern}(?:\.{AnyYearPattern})?\.?",
        $@"{DayPattern}\.\s?{MonthPattern}\.\s?{FourDigitYearPattern}\.?",
        $@"{DayPattern}\.\s{MonthPattern}(?:\.|$)",
        $@"{DayPattern}/{DayPattern}(?:/{AnyYearPattern})?",
        $@"{DayPattern}-{DayPattern}-{AnyYearPattern}",
        $@"{HourPattern}[.:]{MinutePattern}",
        FourDigitYearPattern
    ];

    private static readonly string DateOrTimeToken = string.Join("|", DateOrTimeTokens);

    private static readonly Regex DateOrTimeRun = new(
        $@"^(?:{DateOrTimeToken})(?:[\s\-]+(?:{DateOrTimeToken}))*$",
        RegexOptions.NonBacktracking | RegexOptions.CultureInvariant);

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

    /// <summary>
    /// Reduces a violation text to its category, so a log line never carries the matched health term
    /// (which the employee's own message may have led the model to repeat).
    /// </summary>
    /// <param name="violation">The violation text returned by IsAcceptable</param>
    public static string ToLoggableCategory(string violation) =>
        violation.StartsWith(HealthTermViolationPrefix, StringComparison.Ordinal) ? HealthTermViolationCategory : violation;

    public static bool IsAcceptable(string? question, string? systemInsertedContext, out string violation)
    {
        violation = string.Empty;
        if (string.IsNullOrWhiteSpace(question))
        {
            violation = EmptyViolation;
            return false;
        }

        var text = question.Trim().Normalize(NormalizationForm.FormC);
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
        if (ContainsLink(text))
        {
            violation = LinkViolation;
            return false;
        }

        if (text.Contains(EmailAtSign))
        {
            violation = EmailAddressViolation;
            return false;
        }

        if (ContainsPhoneNumberLikeDigitRun(text))
        {
            violation = PhoneNumberViolation;
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

    /// <summary>
    /// True when the text contains a link marker ("://" or "www."), case-insensitive. Shared between the
    /// question guard and the clarification email reply sender's suspicious-subject check, so both use the
    /// same single source of truth for what counts as a link.
    /// </summary>
    public static bool ContainsLink(string? text) =>
        text != null && LinkMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// True when the text contains a digit run that looks like a phone number (at least
    /// MinPhoneNumberDigits digits, with whitespace, dashes, dots, colons, plus signs or slashes allowed
    /// between them), unless the run is itself a date or time (see class summary). Shared between the question
    /// guard and the clarification email reply sender's suspicious-subject check.
    /// </summary>
    public static bool ContainsPhoneNumberLikeDigitRun(string text) =>
        PhoneNumberDigitRun.Matches(text).Any(match =>
            match.Value.Count(char.IsDigit) >= MinPhoneNumberDigits && !DateOrTimeRun.IsMatch(match.Value));

    public static string? FindHealthTerm(string lowerText)
    {
        var text = RemoveNeutralisedParts(lowerText.Normalize(NormalizationForm.FormC));
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

        return Word.Matches(text)
            .Select(match => match.Value)
            .FirstOrDefault(ClarificationHealthTerms.WholeWordTerms.Contains);
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

        var contextWords = Word.Matches(systemInsertedContext.Normalize(NormalizationForm.FormC).ToLowerInvariant())
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

    private static string RemoveNeutralisedParts(string lowerText)
    {
        var text = lowerText;
        foreach (var phrase in ClarificationHealthTerms.AllowedAbsencePhrases.Concat(ClarificationHealthTerms.HarmlessWordParts))
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
