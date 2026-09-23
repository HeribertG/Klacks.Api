// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Plain-language rendering of an inbound clarification for assistant skills: the status in words (never
/// the internal enum name), one sentence for the skill message, and a compact data object.
/// </summary>
/// <param name="clarification">The clarification related to an analysis</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Services.Inbound;

public static class ClarificationStatusText
{
    private const string OpenText = "waiting for the employee's answer";
    private const string AnsweredText = "answered by the employee";
    private const string UnresolvedText = "answered, but still unclear";
    private const string ExpiredText = "not answered in time";
    private const string TakenOverText = "taken over by a planner";
    private const string SuggestedText = "only suggested to the planners, not sent";
    private const string UnknownText = "unknown";

    public static string Describe(InboundClarificationStatus status) => status switch
    {
        InboundClarificationStatus.Open => OpenText,
        InboundClarificationStatus.Answered => AnsweredText,
        InboundClarificationStatus.Unresolved => UnresolvedText,
        InboundClarificationStatus.Expired => ExpiredText,
        InboundClarificationStatus.TakenOver => TakenOverText,
        InboundClarificationStatus.Suggested => SuggestedText,
        _ => UnknownText
    };

    public static string Sentence(InboundClarification clarification) =>
        $" Klacksy asked the employee back: \"{clarification.Question}\" ({Describe(clarification.Status)}).";

    public static object ToSkillData(InboundClarification clarification) => new
    {
        Status = Describe(clarification.Status),
        clarification.Question,
        clarification.ShiftContext,
        clarification.AskedAt,
        clarification.DeadlineAt,
        clarification.ResolvedAt
    };
}
