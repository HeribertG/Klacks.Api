// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Undoes a recorded macro switch as a whole, identified by exactly one of the switch id, a shift id or an absence type id
/// (the latter two mean the switch that recorded the latest change of that holder), and records the undo in the history.
/// </summary>
/// <param name="SwitchId">Id of the recorded switch</param>
/// <param name="ShiftId">Id of a shift whose latest switch is undone</param>
/// <param name="AbsenceTypeId">Id of an absence type whose latest switch is undone</param>
/// <param name="UserId">Id of the user who asked for the undo</param>

using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Macros;

public record RevertMacroAssignmentCommand(Guid? SwitchId, Guid? ShiftId, Guid? AbsenceTypeId, Guid UserId)
    : IRequest<MacroAssignmentOutcome>;
