// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
