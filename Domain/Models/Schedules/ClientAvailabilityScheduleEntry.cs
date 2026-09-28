// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

public class ClientAvailabilityScheduleEntry
{
    public Guid ClientId { get; set; }
    public DateTime AvailabilityDate { get; set; }
    public string AvailabilityRanges { get; set; } = string.Empty;
}
