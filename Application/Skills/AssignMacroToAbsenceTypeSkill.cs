// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Switches the calculation macro of one absence type, addressed by id, after an administrator confirmed the
/// server-computed preview. The shared body, and the residual risk the owner accepted, live in
/// <see cref="AssignMacroSkillBase"/>.
/// </summary>
/// <param name="absenceTypeId">Required. Id of the absence type whose macro is switched</param>
/// <param name="macroId">Required. Id of the macro the absence type uses from now on</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(MacroAssignmentSkillNames.AssignToAbsenceType)]
public class AssignMacroToAbsenceTypeSkill : AssignMacroSkillBase
{
    public AssignMacroToAbsenceTypeSkill(IMediator mediator)
        : base(mediator)
    {
    }

    protected override MacroAssignmentTarget Target => MacroAssignmentTarget.AbsenceType;
}
