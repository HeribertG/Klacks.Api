// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityResource
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly Date { get; set; }

    public int Hour { get; set; }

    public bool IsAvailable { get; set; }
}
