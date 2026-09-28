// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Response with paginated client list and total count for client availability.
/// </summary>
/// <param name="Clients">List of filtered clients</param>
/// <param name="TotalCount">Total number of matches without paging</param>
namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityClientListResponse
{
    public List<ClientAvailabilityClientResource> Clients { get; set; } = [];
    public int TotalCount { get; set; }
}
