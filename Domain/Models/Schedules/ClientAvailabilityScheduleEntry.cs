// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Schedules;

public class ClientAvailabilityScheduleEntry
{
    public Guid ClientId { get; set; }
    public DateTime AvailabilityDate { get; set; }
    public string AvailabilityRanges { get; set; } = string.Empty;
}
