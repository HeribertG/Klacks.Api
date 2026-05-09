// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client contract for the AutoWizard orchestrator. Only emits a
/// final completion or failure event so the UI can show a single "AutoWizard finished" toast.
/// Intermediate stages (Wizard / Harmonizer / Holistic Harmonizer) run silently.
/// </summary>
public interface IAutoWizardJobClient
{
    Task OnCompleted(AutoWizardJobResultDto result);

    Task OnFailed(string reason);
}
