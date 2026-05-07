// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

public sealed record HolisticHarmonizerRejectionDto(HolisticHarmonizerSwapDto Swap, string Reason, string Detail);
