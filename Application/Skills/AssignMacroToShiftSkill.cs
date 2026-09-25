// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Switches the calculation macro of a shift, addressed by id, together with every other live shift cut from the same
/// order, after an administrator confirmed the server-computed preview. The shared body, and the residual risk the owner
/// accepted, live in <see cref="AssignMacroSkillBase"/>.
/// </summary>
/// <param name="shiftId">Required. Id of any shift of the order whose macro is switched</param>
/// <param name="macroId">Required. Id of the macro the shifts of the order use from now on</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(MacroAssignmentSkillNames.AssignToShift)]
public class AssignMacroToShiftSkill : AssignMacroSkillBase
{
    public AssignMacroToShiftSkill(IMediator mediator)
        : base(mediator)
    {
    }

    protected override MacroAssignmentTarget Target => MacroAssignmentTarget.Shift;
}
