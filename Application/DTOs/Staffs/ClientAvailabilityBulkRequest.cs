// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityBulkRequest
{
    public List<ClientAvailabilityResource> Items { get; set; } = [];
}
