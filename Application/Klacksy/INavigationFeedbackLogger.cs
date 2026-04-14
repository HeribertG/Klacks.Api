// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

/// <summary>
/// Persists telemetry about in-page navigation attempts for the training loop.
/// No-op implementation in Task 7 until the real repository is added in Task 10.
/// </summary>
public interface INavigationFeedbackLogger
{
    Task LogAsync(string rawUtterance, string locale, string? matchedTargetId, double score, string? actualRoute, Guid? userId, CancellationToken ct);
}
