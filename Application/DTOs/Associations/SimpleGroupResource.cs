// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Associations;

public class SimpleGroupResource
{
    public string Description { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public PaymentInterval PaymentInterval { get; set; }

    public Guid? CalendarSelectionId { get; set; }

    public CalendarSelectionResource? CalendarSelection { get; set; }
}
