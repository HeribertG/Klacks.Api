// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

/// <summary>
/// Advances the Klacksy setup-tour state: sets a new lifecycle status and/or marks a station done.
/// </summary>
/// <param name="Status">New lifecycle status, or null to keep the current one</param>
/// <param name="CompletedStation">Station id to mark completed, or null</param>
/// <param name="UserRights">Caller's role/permission claims, populated by the controller</param>
public class SaveOnboardingStateCommand : IRequest<OnboardingResource?>
{
    public string? Status { get; set; }

    public string? CompletedStation { get; set; }

    public IReadOnlyList<string> UserRights { get; set; } = new List<string>();
}
