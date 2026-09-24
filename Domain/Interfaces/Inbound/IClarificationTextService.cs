// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Composes the localized texts of the inbound clarification dialog in the installation language: the
/// planner notices, and the neutral subject of the reply mail to the employee. Times arrive company-local.
/// The original text is shortened here; every other value is inserted as given.
/// </summary>
/// <param name="sender">Sender label shown to planners</param>
/// <param name="summary">Summary of the unclear message</param>
/// <param name="question">The clarification question</param>
/// <param name="shiftContext">Affected shift, or null when none was found in the plan</param>
/// <param name="askedLocal">Company-local time the question was asked</param>
/// <param name="deadlineLocal">Company-local answer deadline</param>
/// <param name="originalText">Raw text of the original message, shortened for display</param>
/// <param name="unresolved">Whether the answer is still unclear</param>
/// <param name="current">The clarification whose status the message arrived after</param>
/// <param name="cancellationToken">Cancels the language lookup</param>

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IClarificationTextService
{
    Task<string> StartedAsync(
        string sender, string summary, string question, string? shiftContext, DateTime deadlineLocal,
        CancellationToken cancellationToken = default);

    Task<string> AnswerContextAsync(
        string question, DateTime askedLocal, string originalText, bool unresolved,
        CancellationToken cancellationToken = default);

    Task<string> ExpiredAsync(
        string sender, string question, DateTime askedLocal, DateTime deadlineLocal, string originalText, string? shiftContext,
        CancellationToken cancellationToken = default);

    Task<string> SendFailedAsync(string question, CancellationToken cancellationToken = default);

    Task<string> SuggestedAsync(string question, CancellationToken cancellationToken = default);

    Task<string> NoPersonalTargetAsync(CancellationToken cancellationToken = default);

    Task<string> AnsweredAfterExpiryAsync(string question, DateTime askedLocal, CancellationToken cancellationToken = default);

    Task<string> ArrivedAfterClosureAsync(
        string question, DateTime askedLocal, InboundClarification current, CancellationToken cancellationToken = default);

    Task<string> NeutralReplySubjectAsync(CancellationToken cancellationToken = default);
}
