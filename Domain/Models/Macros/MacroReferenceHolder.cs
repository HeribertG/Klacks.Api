// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A shift or an absence type seen as the holder of a macro reference, read without tracking. CutGroupKey is the key of a
/// shift's cut group: the order it was copied or cut from, or its own id when it has none (the same key the shift edit
/// skill propagates metadata with).
/// </summary>
/// <param name="Id">Id of the shift or absence type</param>
/// <param name="Target">Whether the holder is a shift or an absence type</param>
/// <param name="Name">Display name: a shift's name, an absence type's first non-empty core-language name</param>
/// <param name="MacroId">Macro the holder uses today; null when it has none</param>
/// <param name="ShiftStatus">Lifecycle status of a shift; null for an absence type</param>
/// <param name="IsScenario">True for a shift that belongs to an analysis scenario</param>
/// <param name="CutGroupId">Id of the order a shift was copied or cut from (Shift.OriginalId); null otherwise</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Macros;

public record MacroReferenceHolder(
    Guid Id,
    MacroAssignmentTarget Target,
    string Name,
    Guid? MacroId,
    ShiftStatus? ShiftStatus,
    bool IsScenario,
    Guid? CutGroupId)
{
    public Guid CutGroupKey => CutGroupId ?? Id;
}
