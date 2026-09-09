// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists navigation-attempt telemetry via the feedback repository.
/// </summary>

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Domain.Models.Klacksy;

public sealed class NavigationFeedbackLogger : INavigationFeedbackLogger
{
    private readonly IKlacksyNavigationFeedbackRepository _repo;

    public NavigationFeedbackLogger(IKlacksyNavigationFeedbackRepository repo) => _repo = repo;

    public Task LogAsync(string rawUtterance, string locale, string? matchedTargetId, double score, string? actualRoute, Guid? userId, CancellationToken ct)
        => _repo.AddAsync(new KlacksyNavigationFeedback
        {
            Utterance = Truncate(rawUtterance),
            Locale = locale,
            MatchedTargetId = matchedTargetId,
            MatchedScore = score > 0 ? score : null,
            UserAction = matchedTargetId == null ? NavigationOutcomeKinds.GaveUp : NavigationOutcomeKinds.AcceptedMatch,
            ActualRoute = actualRoute,
        }, ct);

    public Task LogOutcomeAsync(string? utterance, string locale, string? targetId, string outcome, string route, Guid? userId, CancellationToken ct)
        => _repo.AddAsync(new KlacksyNavigationFeedback
        {
            Utterance = Truncate(utterance ?? string.Empty),
            Locale = locale,
            MatchedTargetId = targetId,
            UserAction = outcome,
            ActualRoute = route,
        }, ct);

    private static string Truncate(string s) => s.Length <= NavigationFeedbackLimits.MaxUtteranceLength
        ? s
        : s[..NavigationFeedbackLimits.MaxUtteranceLength];
}
