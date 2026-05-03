// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// SignalR hub that streams harmonizer job progress. Clients subscribe via <see cref="JoinJob"/>
/// using the JobId returned from /Start. JWT authentication scheme is pinned explicitly
/// because Identity overrides the default scheme to cookies.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class HarmonizerJobHub : Hub<IHarmonizerJobClient>
{
    public Task JoinJob(Guid jobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.HarmonizerGroups.HarmonizerJob(jobId));
    }

    public Task LeaveJob(Guid jobId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRConstants.HarmonizerGroups.HarmonizerJob(jobId));
    }
}
