// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityTotalResource
{
    public Guid ClientId { get; set; }

    public int TotalHours { get; set; }

    public int DaysWithAvailability { get; set; }
}
