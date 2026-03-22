// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resource for a client in the client availability list.
/// </summary>
/// <param name="Id">Client-ID</param>
/// <param name="GroupIds">Associated group IDs</param>
namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityClientResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public bool LegalEntity { get; set; }
    public int IdNumber { get; set; }
    public List<Guid> GroupIds { get; set; } = [];
}
