// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of the clarification check that runs AFTER the regular analysis. QuestionSent means Klacksy
/// asked the employee and already informed the planners, so the adapter must not run the action
/// orchestrator or the regular notification for this message. Otherwise the adapter continues as
/// before and appends NotifierContext (suggested question, send failure, missing contact) when set.
/// </summary>
/// <param name="QuestionSent">True when a question went out and the planners were informed</param>
/// <param name="NotifierContext">Text appended to the regular planner notification, or null</param>

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record ClarificationPostAnalysis(bool QuestionSent, string? NotifierContext)
{
    public static ClarificationPostAnalysis Continue { get; } = new(false, null);

    public static ClarificationPostAnalysis Sent { get; } = new(true, null);

    public static ClarificationPostAnalysis ContinueWith(string context) => new(false, context);
}
