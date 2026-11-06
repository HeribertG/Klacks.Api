// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// SignalR hub that streams wizard job progress to subscribed clients.
/// Clients subscribe to a job via <see cref="JoinJob"/> using the JobId returned from /Start.
/// Connecting (and so <see cref="JoinJob"/>) needs the same role as starting the job (the start endpoint of
/// <c>WizardController</c> and its start skill), because the job events carry employee names and plan details; without it any
/// signed-in user who knew a job id could follow the run.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public sealed class WizardJobHub : Hub<IWizardJobClient>
{
    public Task JoinJob(Guid jobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.WizardGroups.WizardJob(jobId));
    }

    public Task LeaveJob(Guid jobId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRConstants.WizardGroups.WizardJob(jobId));
    }
}
