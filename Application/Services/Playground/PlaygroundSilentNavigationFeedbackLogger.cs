// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decorator that stops the navigation telemetry from storing what Playground visitors typed: the logger writes the
/// raw utterance of every chat message to the database, where it would outlive the visit and mix the shared demo
/// admin's visitors. With the Playground mode off each call is forwarded unchanged.
/// </summary>
/// <param name="inner">The real navigation feedback logger</param>
/// <param name="mode">Tells whether the instance is the Playground</param>

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Domain.Interfaces.Playground;

namespace Klacks.Api.Application.Services.Playground;

public sealed class PlaygroundSilentNavigationFeedbackLogger : INavigationFeedbackLogger
{
    private readonly INavigationFeedbackLogger _inner;
    private readonly IPlaygroundMode _mode;

    public PlaygroundSilentNavigationFeedbackLogger(INavigationFeedbackLogger inner, IPlaygroundMode mode)
    {
        _inner = inner;
        _mode = mode;
    }

    public Task LogAsync(string rawUtterance, string locale, string? matchedTargetId, double score, string? actualRoute, Guid? userId, CancellationToken ct) =>
        _mode.IsActive
            ? Task.CompletedTask
            : _inner.LogAsync(rawUtterance, locale, matchedTargetId, score, actualRoute, userId, ct);

    public Task LogOutcomeAsync(string? utterance, string locale, string? targetId, string outcome, string route, Guid? userId, CancellationToken ct) =>
        _mode.IsActive
            ? Task.CompletedTask
            : _inner.LogOutcomeAsync(utterance, locale, targetId, outcome, route, userId, ct);
}
