// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Settings;

public class CommunicationResource
{
    public Guid ClientId { get; set; }

    public string Description { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string Prefix { get; set; } = string.Empty;

    [Required]
    public CommunicationTypeEnum Type { get; set; }

    [StringLength(100)]
    public string Value { get; set; } = string.Empty;
}
