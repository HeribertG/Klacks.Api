// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
/// Connecting (and so <see cref="JoinJob"/>) needs the same role as starting the job (the start endpoint of
/// <c>AutoWizardController</c> and its start skill), because the job events carry employee names and plan details; without it any
/// signed-in user who knew a job id could follow the run.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public sealed class AutoWizardJobHub : Hub<IAutoWizardJobClient>
{
    public Task JoinJob(Guid jobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.AutoWizardGroups.AutoWizardJob(jobId));
    }

    public Task LeaveJob(Guid jobId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRConstants.AutoWizardGroups.AutoWizardJob(jobId));
    }
}
