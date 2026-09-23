// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Code-side guard rails for a clarification question before it is sent to an employee. The LLM is told
/// the same rules, this guard enforces them: not empty, at most MaxQuestionLength characters, at most
/// MaxQuestionSentences sentences (a period only ends a sentence before whitespace or the end, so a
/// time like 14.00 does not count), phrased as a question (question mark of the language), no link, and
/// no health term of any language from ClarificationHealthTerms. Any violation means no question is
/// sent; the message then stays on the regular path.
/// </summary>
/// <param name="question">The composed question</param>
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

    private static readonly Regex SentenceTerminator = new(
        @"[.!?;;](?=\s|$)|[。！？؟]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly char[] QuestionMarks = ['?', '？', '؟', ';', ';'];

    private static readonly string[] LinkMarkers = ["://", "www."];

    public static bool IsAcceptable(string? question, out string violation)
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

        if (text.IndexOfAny(QuestionMarks) < 0)
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

        var healthTerm = FindHealthTerm(lowerText);
        if (healthTerm != null)
        {
            violation = HealthTermViolationPrefix + healthTerm;
            return false;
        }

        return true;
    }

    public static string? FindHealthTerm(string lowerText)
    {
        foreach (var (language, terms) in ClarificationHealthTerms.ByLanguage)
        {
            var substringMatch = ClarificationHealthTerms.SubstringMatchLanguages.Contains(language);
            foreach (var term in terms)
            {
                var hit = substringMatch
                    ? lowerText.Contains(term, StringComparison.Ordinal)
                    : OccursAtWordStart(lowerText, term);
                if (hit)
                {
                    return term;
                }
            }
        }

        return null;
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
