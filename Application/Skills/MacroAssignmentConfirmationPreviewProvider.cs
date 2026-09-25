// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Confirmation preview of the macro assignment skills: when the autonomy gate holds one of them, this provider computes on
/// the server what the confirmation is about — the holder and, for a shift, the other shifts cut from the same order that
/// are switched with it, the macro before and after, the entries in scope and how many are sealed, the warnings, the
/// notice that nothing is recalculated and a dry run on the most recent open entries — with exactly the parameter reading
/// and planning the confirmed call uses. It refuses (so the gate issues no token) when the caller does not hold the Admin
/// role, an id is missing or invalid, the plan is refused (for an undo: including every conflict) or a target macro cannot
/// run. It only reads. Accepted residual risk (owner decision F1, 2026-09-25): the user sees these facts only in the
/// model's wording, confirm_pending_action can redeem the token in a later turn without an explicit yes, and the chat UI
/// does not show skill results; the confirmed call therefore re-checks the role and plans again.
/// </summary>
/// <param name="planner">Plans the switch or the undo and runs the dry run</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public class MacroAssignmentConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private readonly IMacroAssignmentPlanner _planner;

    public MacroAssignmentConfirmationPreviewProvider(IMacroAssignmentPlanner planner)
    {
        _planner = planner;
    }

    public bool Supports(string skillName) =>
        IsSkill(skillName, MacroAssignmentSkillNames.AssignToShift)
        || IsSkill(skillName, MacroAssignmentSkillNames.AssignToAbsenceType)
        || IsSkill(skillName, MacroAssignmentSkillNames.Revert);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!MacroAssignmentAccess.IsAdmin(context))
        {
            return SkillConfirmationPreview.Refuse(MacroAssignmentAccess.AdminOnlyMessage);
        }

        if (IsSkill(skillName, MacroAssignmentSkillNames.Revert))
        {
            return await BuildRevertAsync(parameters, cancellationToken);
        }

        var target = IsSkill(skillName, MacroAssignmentSkillNames.AssignToShift)
            ? MacroAssignmentTarget.Shift
            : MacroAssignmentTarget.AbsenceType;
        return await BuildAssignAsync(target, parameters, cancellationToken);
    }

    private async Task<SkillConfirmationPreview> BuildAssignAsync(
        MacroAssignmentTarget target, Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var (holderId, macroId, error) = MacroAssignmentParameters.ReadAssign(parameters, target);
        if (error != null)
        {
            return SkillConfirmationPreview.Refuse(error);
        }

        var preview = await _planner.PreviewAssignAsync(target, holderId, macroId, cancellationToken);
        return preview.Refusal != null
            ? SkillConfirmationPreview.Refuse(preview.Refusal)
            : SkillConfirmationPreview.Show(MacroAssignmentTextFormatter.DescribeAssignPreview(preview.Plan, preview.DryRun!));
    }

    private async Task<SkillConfirmationPreview> BuildRevertAsync(
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var (request, error) = MacroAssignmentParameters.ReadRevert(parameters);
        if (error != null)
        {
            return SkillConfirmationPreview.Refuse(error);
        }

        var preview = await _planner.PreviewRevertAsync(request!, cancellationToken);
        return preview.Refusal != null
            ? SkillConfirmationPreview.Refuse(preview.Refusal)
            : SkillConfirmationPreview.Show(MacroAssignmentTextFormatter.DescribeRevertPreview(preview.Plan, preview.DryRun!));
    }

    private static bool IsSkill(string skillName, string expected) =>
        string.Equals(skillName, expected, StringComparison.OrdinalIgnoreCase);
}
