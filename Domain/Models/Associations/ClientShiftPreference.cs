// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Junction entity linking a client to a shift with a preference type (preferred, blacklist).
/// </summary>
/// <param name="ClientId">The client this preference belongs to</param>
/// <param name="ShiftId">The shift being categorized</param>
/// <param name="PreferenceType">The preference category (Preferred, Blacklist)</param>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class ClientShiftPreference : BaseEntity
{
    [Required]
    [ForeignKey("Client")]
    public Guid ClientId { get; set; }

    [Required]
    [ForeignKey("Shift")]
    public Guid ShiftId { get; set; }

    [Required]
    public ShiftPreferenceType PreferenceType { get; set; }

    [JsonIgnore]
    public virtual Client? Client { get; set; }

    [JsonIgnore]
    public virtual Shift? Shift { get; set; }
}
