// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;

namespace Klacks.Api.Application.Services.Schedules.AutoWizard;

/// <summary>
/// Sends SignalR lifecycle events (completed, failed) to an AutoWizard job's client group.
/// Decouples AutoWizardJobRunner from the concrete hub context.
/// </summary>
/// <param name="jobId">Identifies the client group that receives the event.</param>
public interface IAutoWizardHubNotifier
{
    Task NotifyCompletedAsync(Guid jobId, AutoWizardJobResultDto dto);
    Task NotifyFailedAsync(Guid jobId, string reason);
}
