// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Request body for the SetRequiredQualification endpoint: all fields needed to upsert one
/// required-qualification row for a shift.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Associations;

public class SetShiftRequiredQualificationRequest
{
    public Guid ShiftId { get; set; }
    public Guid QualificationId { get; set; }
    public bool IsMandatory { get; set; }
    public QualificationLevel MinLevel { get; set; }
}
