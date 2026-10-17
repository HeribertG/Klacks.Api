// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

/// <summary>
/// Advances a Klacksy tour state: sets a new lifecycle status and/or marks a station done. Kind "setup" (default, kept
/// for older frontends) writes the installation-wide setup tour and needs the admin role; kind "intro" writes the
/// caller's own intro tour and is open to every signed-in user.
/// </summary>
/// <param name="Kind">"setup" or "intro"; empty means "setup"</param>
/// <param name="Status">New lifecycle status (intro additionally accepts the event "offered"), or null to keep the current one</param>
/// <param name="CompletedStation">Station id to mark completed, or null</param>
/// <param name="UserId">Caller's account id, populated by the controller</param>
/// <param name="UserRights">Caller's role/permission claims, populated by the controller</param>
public class SaveOnboardingStateCommand : IRequest<OnboardingResource?>
{
    public string? Kind { get; set; }

    public string? Status { get; set; }

    public string? CompletedStation { get; set; }

    public string UserId { get; set; } = string.Empty;

    public IReadOnlyList<string> UserRights { get; set; } = new List<string>();
}
