// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <summary>
/// Response payload returned immediately from <c>/HolisticHarmonizer/Start</c>. Clients use the
/// <paramref name="JobId"/> to subscribe to the SignalR group and receive subsequent progress
/// and completion events.
/// </summary>
/// <param name="JobId">Server-generated job identifier; same id is broadcast in OnProgress / OnCompleted.</param>
public sealed record StartHolisticHarmonizerResponse(Guid JobId);
