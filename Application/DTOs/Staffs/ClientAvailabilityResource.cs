// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityResource
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly Date { get; set; }

    public int Hour { get; set; }

    public bool IsAvailable { get; set; }
}
