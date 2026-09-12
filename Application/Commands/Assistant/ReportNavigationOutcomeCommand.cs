// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The frontend reporting the real browser-side outcome of a Klacksy navigation it executed
/// (scrolled to the target, target-not-found, or the router refused the navigation for a missing
/// right or a feature that is not activated). Makes the navigation miss rate measurable
/// server-side, which the console.warn it replaces never was.
/// </summary>
/// <param name="UserId">Identity of the caller, taken from the token, never from the body</param>
/// <param name="Route">Route the browser was sent to</param>
/// <param name="Target">data-klacksy-target id that was requested, if any</param>
/// <param name="Outcome">One of NavigationOutcomeKinds.Scrolled/TargetMiss/PermissionDenied/FeatureDisabled</param>
/// <param name="Locale">Locale of the turn that triggered the navigation</param>
/// <param name="Utterance">Optional raw user utterance, truncated before persisting</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class ReportNavigationOutcomeCommand : IRequest<ReportNavigationOutcomeResult>
{
    public string UserId { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string? Target { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public string? Utterance { get; set; }
}

public sealed record ReportNavigationOutcomeResult(bool Recorded);
