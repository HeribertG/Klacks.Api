// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

/// <summary>
/// Temporary no-op logger used until the feedback repository is wired up in Task 10.
/// </summary>
public sealed class NoopNavigationFeedbackLogger : INavigationFeedbackLogger
{
    public Task LogAsync(string rawUtterance, string locale, string? matchedTargetId, double score, string? actualRoute, Guid? userId, CancellationToken ct)
        => Task.CompletedTask;
}
