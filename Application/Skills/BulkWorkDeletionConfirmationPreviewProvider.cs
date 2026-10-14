// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of bulk_delete_works: when the autonomy gate holds the call, this provider resolves it with
/// the very planner the confirmed call uses and lists every Work that would be deleted (date, shift, times), the
/// employee, the range and how a deleted Work can be taken back. It refuses - so the gate issues no token - when the
/// plan is refused (unknown or hidden employee, nothing matching, more than the per-call cap, a locked Work, a sealed
/// day). It only reads. The user sees these facts only in the model's wording; the confirmed call resolves the plan
/// again, so a Work that appeared in the range in the meantime is part of what a confirmed "all in November" deletes.
/// </summary>
/// <param name="planner">Resolves the call's parameters into the Works it would delete</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public class BulkWorkDeletionConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH\\:mm";

    private const string RecomputedNote =
        "This list is the state now: it is worked out again when the call is confirmed, so if works were added or " +
        "changed in the meantime, what is deleted may differ from it.";

    private readonly IBulkWorkDeletionPlanner _planner;

    public BulkWorkDeletionConfirmationPreviewProvider(IBulkWorkDeletionPlanner planner)
    {
        _planner = planner;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, BulkDeleteWorksSkill.SkillName, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planner.PlanAsync(parameters, context.UserLanguage, cancellationToken);
        if (plan.IsRefused)
        {
            return SkillConfirmationPreview.Refuse(plan.Refusal!);
        }

        var lines = plan.Works.Select(work =>
            $"- {work.Date.ToString(DateFormat)} {work.ShiftName} " +
            $"{work.StartTime.ToString(TimeFormat)}-{work.EndTime.ToString(TimeFormat)}");

        var text =
            $"Delete {plan.Works.Count} work(s) of {plan.EmployeeName} between {plan.From.ToString(DateFormat)} and " +
            $"{plan.Until.ToString(DateFormat)}:\n{string.Join("\n", lines)}\n" +
            "The deletion cannot be undone in one step: each work can only be restored one by one, by an " +
            $"administrator at any time and by the deleting user within {WorkRestoreDefaults.UndoWindowSeconds} seconds.\n" +
            RecomputedNote;
        return SkillConfirmationPreview.Show(text);
    }
}
