// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityTotalResource
{
    public Guid ClientId { get; set; }

    public int TotalHours { get; set; }

    public int DaysWithAvailability { get; set; }
}
