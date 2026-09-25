// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Switches the macro reference of one absence type, or of one shift together with every other live shift cut from the
/// same order, and records the switch in the macro assignment history under one switch id.
/// </summary>
/// <param name="Target">Whether HolderId is a shift or an absence type</param>
/// <param name="HolderId">Id of the shift (any shift of the order) or absence type</param>
/// <param name="MacroId">Id of the macro the holder and its cut group use from now on</param>
/// <param name="UserId">Id of the user who asked for the switch</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Macros;

public record AssignMacroCommand(MacroAssignmentTarget Target, Guid HolderId, Guid MacroId, Guid UserId)
    : IRequest<MacroAssignmentOutcome>;
