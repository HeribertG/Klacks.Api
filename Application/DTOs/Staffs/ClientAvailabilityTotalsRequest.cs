// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityTotalsRequest
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public List<Guid> ClientIds { get; set; } = [];
}
