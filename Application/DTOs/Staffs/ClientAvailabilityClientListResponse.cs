// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response mit paginierter Client-Liste und Gesamtanzahl für Client-Availability.
/// </summary>
/// <param name="Clients">Liste der gefilterten Clients</param>
/// <param name="TotalCount">Gesamtanzahl der Treffer ohne Paging</param>
namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityClientListResponse
{
    public List<ClientAvailabilityClientResource> Clients { get; set; } = [];
    public int TotalCount { get; set; }
}
