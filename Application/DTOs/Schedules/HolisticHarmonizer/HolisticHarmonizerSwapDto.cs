// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

public sealed record HolisticHarmonizerSwapDto(int RowA, int DayA, int RowB, int DayB, string Reason);
