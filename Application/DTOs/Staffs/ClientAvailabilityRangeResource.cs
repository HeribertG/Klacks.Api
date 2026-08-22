// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityRangeResource
{
    public Guid ClientId { get; set; }

    public DateOnly Date { get; set; }

    public string Ranges { get; set; } = string.Empty;
}
