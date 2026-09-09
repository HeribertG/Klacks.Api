// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces.Klacksy;

/// <summary>
/// Persists telemetry about in-page navigation attempts for the training loop.
/// No-op implementation in Task 7 until the real repository is added in Task 10.
/// </summary>
public interface INavigationFeedbackLogger
{
    Task LogAsync(string rawUtterance, string locale, string? matchedTargetId, double score, string? actualRoute, Guid? userId, CancellationToken ct);

    /// <summary>
    /// Logs the outcome of a single navigation as a second feedback row for the same turn: the
    /// browser-reported outcome of a navigate_to it executed (scrolled/target-miss/permission-denied),
    /// or the server-detected suspected-miss when the model navigated without a target.
    /// </summary>
    Task LogOutcomeAsync(string? utterance, string locale, string? targetId, string outcome, string route, Guid? userId, CancellationToken ct);
}
