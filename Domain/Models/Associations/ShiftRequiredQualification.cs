// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Junction entity: a shift requires a qualification at a minimum level. When IsMandatory is true
/// an employee without the qualification (at MinLevel or higher) is ineligible for the shift; when
/// false it is a soft preference signal only.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class ShiftRequiredQualification : BaseEntity
{
    [Required]
    [ForeignKey("Shift")]
    public Guid ShiftId { get; set; }

    [Required]
    [ForeignKey("Qualification")]
    public Guid QualificationId { get; set; }

    [Required]
    public bool IsMandatory { get; set; }

    [Required]
    public QualificationLevel MinLevel { get; set; }

    [JsonIgnore]
    public virtual Shift? Shift { get; set; }

    [JsonIgnore]
    public virtual Qualification? Qualification { get; set; }
}
