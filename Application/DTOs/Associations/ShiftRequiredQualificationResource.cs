// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO for a shift's required qualification: which qualification is needed, whether it is
/// mandatory (hard eligibility gate) and the minimum acceptable proficiency level.
/// </summary>
/// <param name="Id">Row identifier</param>
/// <param name="ShiftId">The shift that requires this qualification</param>
/// <param name="QualificationId">The required qualification</param>
/// <param name="IsMandatory">True = hard gate; false = soft preference only</param>
/// <param name="MinLevel">Minimum proficiency level accepted</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Associations;

public class ShiftRequiredQualificationResource
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid QualificationId { get; set; }
    public bool IsMandatory { get; set; }
    public QualificationLevel MinLevel { get; set; }
}
