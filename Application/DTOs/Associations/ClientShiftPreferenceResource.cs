// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO for client shift preference with shift display information.
/// </summary>
/// <param name="ClientId">The client this preference belongs to</param>
/// <param name="ShiftId">The referenced shift</param>
/// <param name="PreferenceType">Category: Preferred or Blacklist</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Associations;

public class ClientShiftPreferenceResource
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Guid ShiftId { get; set; }

    public ShiftPreferenceType PreferenceType { get; set; }

    public string? ShiftName { get; set; }

    public string? ShiftAbbreviation { get; set; }
}
