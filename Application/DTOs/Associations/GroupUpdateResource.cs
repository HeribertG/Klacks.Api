// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Domain.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Associations;

public class GroupUpdateResource
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    public Guid? Parent { get; set; }

    public ICollection<Guid> ClientIds { get; set; } = new Collection<Guid>();

    public PaymentInterval PaymentInterval { get; set; }

    public Guid? CalendarSelectionId { get; set; }
}
