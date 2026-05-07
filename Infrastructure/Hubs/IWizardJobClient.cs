// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client contract for wizard job progress streams.
/// </summary>
public interface IWizardJobClient
{
    Task OnProgress(WizardJobProgressDto progress);

    Task OnCompleted(WizardJobResultDto result);

    Task OnCancelled();

    Task OnFailed(string reason);
}
