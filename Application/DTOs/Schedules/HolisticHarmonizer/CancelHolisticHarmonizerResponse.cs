// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="Cancelled">True if the job was found in the registry and a cancellation was issued.</param>
public sealed record CancelHolisticHarmonizerResponse(bool Cancelled);
