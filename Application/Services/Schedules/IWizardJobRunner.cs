// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Coordinates background execution of wizard GA jobs and exposes cancellation/status queries.
/// </summary>
public interface IWizardJobRunner
{
    /// <summary>Starts a new job in the background. Returns the job id immediately.</summary>
    Task<Guid> StartAsync(WizardContextRequest request, CancellationToken externalCt);

    /// <summary>Cancels a running job. Returns false if the job is unknown or already finished.</summary>
    bool TryCancel(Guid jobId);

    /// <summary>Tells whether the job is still running.</summary>
    bool IsRunning(Guid jobId);
}
