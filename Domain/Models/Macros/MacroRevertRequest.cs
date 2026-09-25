// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// Identifies the macro switch to undo: exactly one of the switch id, a shift id or an absence type id (the latter two
/// mean the switch that recorded the latest change of that holder). The whole switch is undone.
/// </summary>
/// <param name="SwitchId">Id of the recorded switch, as reported when the macro was switched</param>
/// <param name="ShiftId">Id of a shift whose latest switch is undone</param>
/// <param name="AbsenceTypeId">Id of an absence type whose latest switch is undone</param>
public record MacroRevertRequest(Guid? SwitchId, Guid? ShiftId, Guid? AbsenceTypeId)
{
    public int SelectorCount =>
        (SwitchId.HasValue ? 1 : 0) + (ShiftId.HasValue ? 1 : 0) + (AbsenceTypeId.HasValue ? 1 : 0);
}
