// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates and persists a browser-reported navigation outcome as a second
/// klacksy_navigation_feedback row for the turn. An unknown outcome is rejected as a bad request,
/// because that is a client programming error rather than telemetry noise. Only the three
/// browser-observable outcomes are accepted here - suspected-miss is logged directly by
/// ChatController, never reported by the client.
/// </summary>
/// <param name="logger">Feedback store, self-committing</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ReportNavigationOutcomeCommandHandler
    : IRequestHandler<ReportNavigationOutcomeCommand, ReportNavigationOutcomeResult>
{
    private static readonly HashSet<string> KnownClientOutcomes = new()
    {
        NavigationOutcomeKinds.Scrolled,
        NavigationOutcomeKinds.TargetMiss,
        NavigationOutcomeKinds.PermissionDenied,
    };

    private readonly INavigationFeedbackLogger _logger;

    public ReportNavigationOutcomeCommandHandler(INavigationFeedbackLogger logger) => _logger = logger;

    public async Task<ReportNavigationOutcomeResult> Handle(
        ReportNavigationOutcomeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (!KnownClientOutcomes.Contains(request.Outcome))
        {
            throw new ArgumentException(
                $"Outcome must be one of '{string.Join("', '", KnownClientOutcomes)}' but was '{request.Outcome}'.",
                nameof(request));
        }

        var userId = Guid.TryParse(request.UserId, out var parsedUserId) ? parsedUserId : (Guid?)null;

        await _logger.LogOutcomeAsync(
            Truncate(request.Utterance),
            request.Locale,
            request.Target,
            request.Outcome,
            request.Route,
            userId,
            cancellationToken);

        return new ReportNavigationOutcomeResult(Recorded: true);
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= NavigationFeedbackLimits.MaxUtteranceLength
            ? value
            : value[..NavigationFeedbackLimits.MaxUtteranceLength];
    }
}
