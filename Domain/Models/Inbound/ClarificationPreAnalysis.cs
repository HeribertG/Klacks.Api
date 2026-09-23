// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of the clarification check that runs BEFORE the regular analysis of an inbound message.
/// AnswerAnalysis is set when the message answered an open clarification: it is the re-analysis of
/// original message, question and answer, and replaces the regular analysis. NotifierContext is an
/// optional block the planner notification appends (answer history, or a note that an earlier
/// question had expired).
/// </summary>
/// <param name="AnswerAnalysis">Re-analysis of the answered clarification, or null</param>
/// <param name="NotifierContext">Text appended to the planner notification, or null</param>

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record ClarificationPreAnalysis(InboundAnalysis? AnswerAnalysis, string? NotifierContext)
{
    public static ClarificationPreAnalysis None { get; } = new(null, null);

    public static ClarificationPreAnalysis Context(string context) => new(null, context);

    public static ClarificationPreAnalysis Answer(InboundAnalysis analysis, string context) => new(analysis, context);
}
