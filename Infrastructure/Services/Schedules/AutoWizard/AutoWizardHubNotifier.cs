// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// Sends OnCompleted and OnFailed SignalR events to AutoWizard job groups.
/// Wraps IHubContext to isolate SignalR coupling from AutoWizardJobRunner.
/// </summary>
public class AutoWizardHubNotifier : IAutoWizardHubNotifier
{
    private readonly IHubContext<AutoWizardJobHub, IAutoWizardJobClient> _hubContext;
    private readonly ILogger<AutoWizardHubNotifier> _logger;

    public AutoWizardHubNotifier(
        IHubContext<AutoWizardJobHub, IAutoWizardJobClient> hubContext,
        ILogger<AutoWizardHubNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyCompletedAsync(Guid jobId, AutoWizardJobResultDto dto)
    {
        try
        {
            await _hubContext.Clients.Group(SignalRConstants.AutoWizardGroups.AutoWizardJob(jobId)).OnCompleted(dto);
        }
        catch (Exception ex)
        {
            // A broadcast failure must not propagate into the caller's failure path, which would report
            // a finished chain as failed.
            _logger.LogError(ex, "AutoWizard - failed to send OnCompleted event to client group");
        }
    }

    public async Task NotifyFailedAsync(Guid jobId, string reason)
    {
        try
        {
            await _hubContext.Clients.Group(SignalRConstants.AutoWizardGroups.AutoWizardJob(jobId)).OnFailed(reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoWizard - failed to send OnFailed event to client group");
        }
    }
}
