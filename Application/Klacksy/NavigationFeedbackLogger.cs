// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists navigation-attempt telemetry via the feedback repository.
/// </summary>

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Domain.Models.Klacksy;
using Klacks.Api.Infrastructure.Repositories.Klacksy;

public sealed class NavigationFeedbackLogger : INavigationFeedbackLogger
{
    private const int MaxUtteranceLength = 500;
    private const string UserActionAccepted = "accepted-match";
    private const string UserActionGaveUp = "gave-up";

    private readonly IKlacksyNavigationFeedbackRepository _repo;

    public NavigationFeedbackLogger(IKlacksyNavigationFeedbackRepository repo) => _repo = repo;

    public Task LogAsync(string rawUtterance, string locale, string? matchedTargetId, double score, string? actualRoute, Guid? userId, CancellationToken ct)
        => _repo.AddAsync(new KlacksyNavigationFeedback
        {
            Utterance = Truncate(rawUtterance, MaxUtteranceLength),
            Locale = locale,
            MatchedTargetId = matchedTargetId,
            MatchedScore = score > 0 ? score : null,
            UserAction = matchedTargetId == null ? UserActionGaveUp : UserActionAccepted,
            ActualRoute = actualRoute,
        }, ct);

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
