// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Undoes a recorded macro switch as a whole, identified by its switch id, and records the undo in the history.
/// </summary>
/// <param name="SwitchId">Id of the recorded switch</param>
/// <param name="UserId">Id of the user who asked for the undo</param>

using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Macros;

public record RevertMacroAssignmentCommand(Guid SwitchId, Guid UserId) : IRequest<MacroAssignmentOutcome>;
