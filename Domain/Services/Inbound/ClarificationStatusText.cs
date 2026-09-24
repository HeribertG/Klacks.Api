// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Plain-language rendering of an inbound clarification for assistant skills: the status in words (never
/// the internal enum name), one sentence for the skill message, and a compact data object. Everything here
/// is English on purpose: the text goes to the language model, which answers the user in the user's own
/// language. KeyOf maps a clarification to its catalogue key so planner notices can name the same status in
/// the installation language (ClarificationTextService); the English words are the catalogue's English
/// entries, so the two never drift.
/// </summary>
/// <param name="clarification">The clarification related to an analysis</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Services.Inbound;

public static class ClarificationStatusText
{
    private const char DoubleQuote = '"';
    private const char SingleQuote = '\'';

    public static string KeyOf(InboundClarificationStatus status) => status switch
    {
        InboundClarificationStatus.Open => ClarificationTextKeys.StatusOpen,
        InboundClarificationStatus.Answered => ClarificationTextKeys.StatusAnswered,
        InboundClarificationStatus.Unresolved => ClarificationTextKeys.StatusUnresolved,
        InboundClarificationStatus.Expired => ClarificationTextKeys.StatusExpired,
        InboundClarificationStatus.TakenOver => ClarificationTextKeys.StatusTakenOver,
        InboundClarificationStatus.Suggested => ClarificationTextKeys.StatusSuggested,
        _ => ClarificationTextKeys.StatusUnknown
    };

    public static string KeyOf(InboundClarification clarification) =>
        IsUndelivered(clarification) ? ClarificationTextKeys.StatusUndelivered : KeyOf(clarification.Status);

    public static string Describe(InboundClarificationStatus status) => ClarificationTexts.English(KeyOf(status));

    public static string Describe(InboundClarification clarification) => ClarificationTexts.English(KeyOf(clarification));

    public static string Sentence(InboundClarification clarification)
    {
        var question = clarification.Question.Replace(DoubleQuote, SingleQuote);
        var status = Describe(clarification);

        if (clarification.Status == InboundClarificationStatus.Suggested)
        {
            return $" Klacksy would ask the employee: \"{question}\" ({status}).";
        }

        if (IsUndelivered(clarification))
        {
            return $" Klacksy tried to ask the employee: \"{question}\" ({status}).";
        }

        return $" Klacksy asked the employee back: \"{question}\" ({status}).";
    }

    public static object ToSkillData(InboundClarification clarification) => new
    {
        clarification.Id,
        Status = Describe(clarification),
        clarification.Question,
        clarification.ShiftContext,
        clarification.AskedAt,
        clarification.DeadlineAt,
        clarification.ResolvedAt
    };

    private static bool IsUndelivered(InboundClarification clarification) =>
        clarification.Status == InboundClarificationStatus.Unresolved && clarification.AnswerSourceId == null;
}
