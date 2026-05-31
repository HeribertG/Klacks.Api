// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Junction entity: an employee holds a qualification at a given level, optionally time-bounded
/// (a certificate that expires). The effective qualification at a date D is the active row with
/// ValidFrom &lt;= D and (ValidUntil is null or ValidUntil &gt;= D).
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class ClientQualification : BaseEntity
{
    [Required]
    [ForeignKey("Client")]
    public Guid ClientId { get; set; }

    [Required]
    [ForeignKey("Qualification")]
    public Guid QualificationId { get; set; }

    [Required]
    public QualificationLevel Level { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidUntil { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [JsonIgnore]
    public virtual Client? Client { get; set; }

    [JsonIgnore]
    public virtual Qualification? Qualification { get; set; }
}
