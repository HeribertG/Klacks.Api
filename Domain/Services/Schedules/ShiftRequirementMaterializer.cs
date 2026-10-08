// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Turns requirement rows that a shift only inherits (<see cref="ShiftRequirementSourceResolver"/>) into rows of its own.
/// Needed wherever a planner writes the FIRST own row of a shift: under the nearest-wins rule that row would otherwise
/// silently replace every inherited requirement (the planner sees only own rows and meant an addition, not a swap), and
/// wherever the cut dialog creates a piece, so the piece shows what applies to it. The copies are new rows (fresh ids),
/// never the stored source rows, because the (shift, qualification) pair is unique per shift.
/// </summary>
public static class ShiftRequirementMaterializer
{
    /// <summary>
    /// Copies of the rows <paramref name="shiftId"/> inherits, as rows of its own; empty when the shift already carries
    /// rows of its own (nothing is inherited then) or when nothing applies to it.
    /// </summary>
    /// <param name="effective">Effective rows as resolved for the shift (other shifts' entries are ignored)</param>
    /// <param name="shiftId">Shift that is about to receive its first own row</param>
    public static List<ShiftRequiredQualification> InheritedAsOwn(
        IEnumerable<EffectiveShiftRequirement> effective,
        Guid shiftId)
    {
        var forShift = effective.Where(e => e.ShiftId == shiftId).ToList();
        if (forShift.Any(e => e.SourceShiftId == shiftId))
        {
            return [];
        }

        return CopyAll(forShift.Select(e => e.Requirement), shiftId);
    }

    /// <summary>
    /// New rows carrying the qualification, mandatory flag and level of <paramref name="sources"/> for
    /// <paramref name="targetShiftId"/>; one row per qualification.
    /// </summary>
    /// <param name="sources">Rows to copy</param>
    /// <param name="targetShiftId">Shift the copies belong to</param>
    public static List<ShiftRequiredQualification> CopyAll(
        IEnumerable<ShiftRequiredQualification> sources,
        Guid targetShiftId)
        => sources
            .GroupBy(source => source.QualificationId)
            .Select(group => Copy(group.First(), targetShiftId))
            .ToList();

    /// <summary>A new row with the qualification, mandatory flag and level of <paramref name="source"/>.</summary>
    /// <param name="source">Row to copy</param>
    /// <param name="targetShiftId">Shift the copy belongs to</param>
    public static ShiftRequiredQualification Copy(ShiftRequiredQualification source, Guid targetShiftId) => new()
    {
        Id = Guid.NewGuid(),
        ShiftId = targetShiftId,
        QualificationId = source.QualificationId,
        IsMandatory = source.IsMandatory,
        MinLevel = source.MinLevel,
    };
}
