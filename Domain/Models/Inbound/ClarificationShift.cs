// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record ClarificationShift(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ShiftName);
