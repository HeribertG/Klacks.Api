// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Clients;

public class ClientListItemResource
{
    public string Id { get; set; } = string.Empty;
    public int IdNumber { get; set; }
    public string? Company { get; set; } = string.Empty;
    public string? FirstName { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public GenderEnum Gender { get; set; }
    public DateTime? Birthdate { get; set; }
    public bool IsDeleted { get; set; }
}
