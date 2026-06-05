// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientQualificationResource
{
    public Guid Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public Guid QualificationId { get; set; }

    [Required]
    public QualificationLevel Level { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidUntil { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
