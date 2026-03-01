// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientForReplacementResource
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? Company { get; set; }
    public bool LegalEntity { get; set; }
    public int IdNumber { get; set; }
    public List<Guid> GroupIds { get; set; } = [];
}
