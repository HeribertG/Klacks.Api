// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityTotalsRequest
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public List<Guid> ClientIds { get; set; } = [];
}
