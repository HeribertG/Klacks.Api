// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record HeartbeatDataSnapshot(
    int NewAbsenceRequests,
    int WorkEntriesCreatedToday,
    int NewScheduleChanges,
    DateTime Since);
