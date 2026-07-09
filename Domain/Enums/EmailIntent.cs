// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum EmailIntent
{
    Unknown = 0,
    CustomerMessage = 1,
    WorkCancellation = 2,
    VacationRequest = 3,
    DayOffWish = 4,
    Other = 5,
    AvailabilityAnnouncement = 6,
    ShiftPreference = 7
}
