// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.DTOs.Associations;

namespace Klacks.Api.Application.DTOs.Schedules;

public class ClientBreakPlaceholderResource
{
    public ICollection<BreakPlaceholderResource> BreakPlaceholders { get; set; } = [];

    public string? Company { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    public GenderEnum Gender { get; set; }

    public Guid Id { get; set; }

    public int IdNumber { get; set; }

    public bool LegalEntity { get; set; }

    public string? MaidenName { get; set; } = string.Empty;

    public MembershipResource? Membership { get; set; }

    public string? Name { get; set; } = string.Empty;

    public string? SecondName { get; set; } = string.Empty;

    public string? Title { get; set; } = string.Empty;

    public int Type { get; set; }
}
