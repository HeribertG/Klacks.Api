// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// Sends AutofillRunStarted and AutofillRunEnded over the schedule hub to every open schedule whose user may see the
/// run's group, whatever group, period or scenario it shows; the client decides whether the run concerns its view.
/// Built from singletons only and filtered by the visibility cached on each connection, because the end of a run is
/// announced from the background task, where no calling user exists and a visibility guard would be fail-open.
/// </summary>
/// <param name="hubContext">Typed context of the schedule hub</param>
/// <param name="dateRangeTracker">Open schedule connections with their cached visibility</param>
/// <param name="logger">Logs how many connections were told</param>
public sealed class AutofillRunPresenceNotifier : IAutofillRunPresenceNotifier
{
    private readonly IHubContext<WorkNotificationHub, IScheduleClient> _hubContext;
    private readonly IConnectionDateRangeTracker _dateRangeTracker;
    private readonly ILogger<AutofillRunPresenceNotifier> _logger;

    public AutofillRunPresenceNotifier(
        IHubContext<WorkNotificationHub, IScheduleClient> hubContext,
        IConnectionDateRangeTracker dateRangeTracker,
        ILogger<AutofillRunPresenceNotifier> logger)
    {
        _hubContext = hubContext;
        _dateRangeTracker = dateRangeTracker;
        _logger = logger;
    }

    public Task NotifyStartedAsync(AutofillRunPresenceDto run)
        => SendAsync(run, (clients, dto) => clients.AutofillRunStarted(dto), nameof(IScheduleClient.AutofillRunStarted));

    public Task NotifyEndedAsync(AutofillRunPresenceDto run)
        => SendAsync(run, (clients, dto) => clients.AutofillRunEnded(dto), nameof(IScheduleClient.AutofillRunEnded));

    private async Task SendAsync(
        AutofillRunPresenceDto run,
        Func<IScheduleClient, AutofillRunPresenceDto, Task> send,
        string eventName)
    {
        var connectionIds = (await _dateRangeTracker.GetConnectionsForGroupAudienceAsync(run.GroupId))
            .Select(connection => connection.ConnectionId)
            .ToList();

        if (connectionIds.Count == 0)
        {
            _logger.LogDebug("SignalR SKIP: {Event} for run {RunId} - no open schedule may see group {GroupId}",
                eventName, run.RunId, run.GroupId);
            return;
        }

        await send(_hubContext.Clients.Clients(connectionIds), run);
        _logger.LogDebug("Sent {Event} for run {RunId} to {Count} connections", eventName, run.RunId, connectionIds.Count);
    }
}
