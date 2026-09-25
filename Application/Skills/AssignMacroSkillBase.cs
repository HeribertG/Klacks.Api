// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared body of the two macro assignment skills. The caller must hold the Admin role itself (CanEditSettings is not
/// enough). Holder and macro are addressed by id only, never by name; a shift is switched together with every other live
/// shift cut from the same order (owner decision F2). The skill only sends <see cref="AssignMacroCommand"/>: its handler
/// plans the switch and runs the dry run exactly like the confirmation preview did, once per call — a refusal, including a
/// target macro that cannot run, comes back as an error and nothing is written — then switches the references and records
/// one history row per written holder under one switch id. Nothing is recalculated; the answer says so, repeats the entry
/// counts of that dry run and carries the switch id that undoes the whole switch. A database failure at the commit is
/// answered without the raw database message and without claiming that nothing changed. The confirmation itself is
/// enforced by the autonomy gate (the skills are Sensitive). Accepted residual risk (owner decision F1, 2026-09-25): the
/// user reads the confirmation in the language model's wording of the server-computed preview, confirm_pending_action can
/// redeem the token in a later turn without an explicit yes, and the chat UI does not show skill results; that is why every
/// confirmed call checks the role here and the handler plans again instead of trusting the preview it was confirmed with.
/// </summary>
/// <param name="mediator">Dispatches the switch command</param>

using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

public abstract class AssignMacroSkillBase : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    protected AssignMacroSkillBase(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected abstract MacroAssignmentTarget Target { get; }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!MacroAssignmentAccess.IsAdmin(context))
        {
            return SkillResult.Error(MacroAssignmentAccess.AdminOnlyMessage);
        }

        var (holderId, macroId, parameterError) = MacroAssignmentParameters.ReadAssign(parameters, Target);
        if (parameterError != null)
        {
            return SkillResult.Error(parameterError);
        }

        var (outcome, error) = await MacroAssignmentCommandSender.SendAsync(
            _mediator, new AssignMacroCommand(Target, holderId, macroId, context.UserId), cancellationToken);
        if (outcome == null)
        {
            return SkillResult.Error(error!);
        }

        return SkillResult.SuccessResult(
            new
            {
                outcome.SwitchId,
                HolderId = outcome.Holder.Id,
                SwitchedHolderIds = outcome.Changes.Select(change => change.Holder.Id).ToList(),
                NewMacroId = macroId,
                outcome.DryRun.TotalEntries,
                outcome.DryRun.SealedEntries,
                outcome.DryRun.ChangedSamples
            },
            MacroAssignmentTextFormatter.DescribeAssigned(outcome));
    }
}
