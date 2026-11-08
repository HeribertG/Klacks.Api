// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for the AutoWizard orchestrator that runs Wizard 1, Harmonizer (Wizard 2)
/// and Holistic Harmonizer (Wizard 3) sequentially in the backend. Clients subscribe via
/// <see cref="JoinJob"/> using the JobId returned from /Start. JWT authentication scheme is
/// pinned explicitly because Identity overrides the default scheme to cookies.
/// Connecting needs the planning right CanEditSchedule (policy RequireScheduleEditing), like the start endpoint of
/// <c>AutoWizardController</c> and its start skill (autofill is planning work). Because the job events carry employee
/// names and plan details, <see cref="JoinJob"/> joins only a job the caller may address (<see cref="IAutoWizardJobAccessGuard"/>: unrestricted callers every job, restricted callers the
/// jobs of their visible groups); any other job id, and a connection without a user id, is ignored like an unknown job.
/// The check runs as the connection's own user (HubCallerPrincipal), never as an empty request context.
/// </summary>
/// <param name="jobAccessGuard">Decides whether the caller may follow a job.</param>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthorizationPolicies.RequireScheduleEditing)]
public sealed class AutoWizardJobHub : Hub<IAutoWizardJobClient>
{
    private readonly IAutoWizardJobAccessGuard _jobAccessGuard;

    public AutoWizardJobHub(IAutoWizardJobAccessGuard jobAccessGuard)
    {
        _jobAccessGuard = jobAccessGuard;
    }

    public async Task JoinJob(Guid jobId)
    {
        using var caller = HubCallerPrincipal.Begin(Context);
        if (caller is null || !await _jobAccessGuard.CanAccessAsync(jobId, Context.ConnectionAborted))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.AutoWizardGroups.AutoWizardJob(jobId));
    }

    public Task LeaveJob(Guid jobId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRConstants.AutoWizardGroups.AutoWizardJob(jobId));
    }
}
