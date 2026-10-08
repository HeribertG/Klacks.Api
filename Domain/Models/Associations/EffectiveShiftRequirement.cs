// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Associations;

/// <summary>
/// A stored requirement row that applies to a staffed shift, possibly one set on another shift of its order tree
/// (see <see cref="Klacks.Api.Domain.Services.Schedules.ShiftRequirementSourceResolver"/>).
/// </summary>
/// <param name="ShiftId">Staffed shift the requirement applies to</param>
/// <param name="ShiftName">Display name of the staffed shift (its Name, or its Abbreviation when the name is empty)</param>
/// <param name="SourceShiftId">Shift that carries the row; equals ShiftId when the shift has rows of its own</param>
/// <param name="Requirement">The stored row, unchanged and untracked (ShiftId = SourceShiftId), with its Qualification</param>
public sealed record EffectiveShiftRequirement(
    Guid ShiftId,
    string ShiftName,
    Guid SourceShiftId,
    ShiftRequiredQualification Requirement);
