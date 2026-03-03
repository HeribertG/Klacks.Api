// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class Membership : BaseEntity
{
    [JsonIgnore]
    public virtual Client Client { get; set; } = null!;

    [Required]
    [ForeignKey("Employee")]
    public Guid ClientId { get; set; }

    public int Type { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }
}
