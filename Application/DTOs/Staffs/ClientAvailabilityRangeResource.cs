// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityRangeResource
{
    public Guid ClientId { get; set; }

    public DateOnly Date { get; set; }

    public string Ranges { get; set; } = string.Empty;
}
