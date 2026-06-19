// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;

namespace Klacks.Api.Application.Interfaces.Schedules.AutoWizard;

/// <summary>
/// Background orchestrator that runs Wizard 1 (Planner), Harmonizer (Wizard 2) and
/// Holistic Harmonizer (Wizard 3) sequentially in a single fire-and-forget job. Streams a
/// single OnCompleted (or OnFailed) event over the AutoWizard SignalR hub when the chain
/// terminates. Internal stage progress events are intentionally suppressed.
/// </summary>
public interface IAutoWizardJobRunner
{
    Task<Guid> StartAsync(StartAutoWizardRequest request, CancellationToken externalCt);

    bool TryCancel(Guid jobId);

    bool IsRunning(Guid jobId);
}
