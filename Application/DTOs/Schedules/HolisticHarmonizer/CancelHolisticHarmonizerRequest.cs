// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="JobId">Identifier of the running job to cancel; obtained from <c>/Start</c>.</param>
public sealed record CancelHolisticHarmonizerRequest(Guid JobId);
