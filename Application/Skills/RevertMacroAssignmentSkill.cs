// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Undoes a macro switch the assistant made as a whole and puts each switched shift (or the absence type) back on its
/// previous macro (owner decisions F2 and F6: one switch of a shift covers every cut of its order, and an undo itself is
/// final). The caller must hold the Admin role itself. The switch is identified by exactly one of switchId, shiftId or
/// absenceTypeId (the latter two mean the switch that recorded the latest change of that holder when the confirmed call
/// runs). The skill only sends <see cref="RevertMacroAssignmentCommand"/> with the selector unchanged: its handler resolves
/// the switch, plans the undo and runs the dry run exactly like the confirmation preview did, once per call — a refusal
/// (unknown, itself an undo, already undone, or any shift of the switch in conflict: switched again later, changed
/// elsewhere, macro to restore deleted or unable to run; the refusal lists the conflicts) comes back as an error and
/// nothing is written. The answer carries the undo id, the id of the undone switch and the entry counts of that dry run.
/// Nothing is recalculated. A database failure at the commit is answered without the raw database message and without
/// claiming that nothing changed. The confirmation itself is enforced by the autonomy gate (the skill is Sensitive).
/// Accepted residual risk (owner decision F1, 2026-09-25): the user reads the confirmation in the language model's
/// wording, confirm_pending_action can redeem the token in a later turn without an explicit yes, and the chat UI does not
/// show skill results; that is why every confirmed call checks the role here and the handler plans again.
/// </summary>
/// <param name="switchId">Optional. Id of the recorded switch, as reported when the macro was switched</param>
/// <param name="shiftId">Optional. Undo the latest switch that changed this shift</param>
/// <param name="absenceTypeId">Optional. Undo the latest switch that changed this absence type</param>

using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(MacroAssignmentSkillNames.Revert)]
public class RevertMacroAssignmentSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public RevertMacroAssignmentSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!MacroAssignmentAccess.IsAdmin(context))
        {
            return SkillResult.Error(MacroAssignmentAccess.AdminOnlyMessage);
        }

        var (request, parameterError) = MacroAssignmentParameters.ReadRevert(parameters);
        if (parameterError != null)
        {
            return SkillResult.Error(parameterError);
        }

        var command = new RevertMacroAssignmentCommand(
            request!.SwitchId, request.ShiftId, request.AbsenceTypeId, context.UserId);
        var (outcome, error) = await MacroAssignmentCommandSender.SendAsync(_mediator, command, cancellationToken);
        if (outcome == null)
        {
            return SkillResult.Error(error!);
        }

        return SkillResult.SuccessResult(
            new
            {
                UndoId = outcome.SwitchId,
                outcome.UndoneSwitchId,
                HolderId = outcome.Holder.Id,
                RestoredHolderIds = outcome.Changes.Select(change => change.Holder.Id).ToList(),
                outcome.DryRun.TotalEntries,
                outcome.DryRun.SealedEntries,
                outcome.DryRun.ChangedSamples
            },
            MacroAssignmentTextFormatter.DescribeReverted(outcome));
    }
}
