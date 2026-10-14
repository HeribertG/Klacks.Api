// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of bulk_delete_works: when the autonomy gate holds the call, this provider resolves it with
/// the very planner the confirmed call uses and lists every Work that would be deleted (date, shift, times), the
/// employee, the range and how a deleted Work can be taken back. It refuses - so the gate issues no token - when the
/// plan is refused (unknown or hidden employee, nothing matching, more than the per-call cap, a locked Work, a sealed
/// day). The container entries (child works and breaks) that go with the listed works are counted in the
/// headline. It only reads. The user sees these facts only in the model's wording. The ids of the listed Works are returned as
/// the preview's binding (BulkWorkDeletionBinding, container entries included) and stored with the token: the confirmed
/// call deletes exactly these Works and entries or nothing, so a Work or entry that appeared in the meantime is never part
/// of what was confirmed.
/// </summary>
/// <param name="planner">Resolves the call's parameters into the Works it would delete</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public class BulkWorkDeletionConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH\\:mm";

    private const string BoundNote =
        "Exactly these works and their container entries are bound to the confirmation: if any of them is gone or another " +
        "one appears (a work in the range, or an entry under one of the listed works) before the user confirms, nothing " +
        "is deleted and a fresh preview is shown.";

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
            $"Delete {plan.Works.Count} work(s) of {plan.EmployeeName}{DescribeContainerEntries(plan)} between " +
            $"{plan.From.ToString(DateFormat)} and {plan.Until.ToString(DateFormat)}:\n{string.Join("\n", lines)}\n" +
            "The deletion cannot be undone in one step: each work can only be restored one by one, by an " +
            $"administrator at any time and by the deleting user within {WorkRestoreDefaults.UndoWindowSeconds} seconds.\n" +
            BoundNote;
        return SkillConfirmationPreview.Show(text, BulkWorkDeletionBinding.Of(plan));
    }

    private static string DescribeContainerEntries(BulkWorkDeletionPlan plan) => plan.ChildEntryCount switch
    {
        0 => string.Empty,
        1 => ", together with 1 container entry,",
        var count => $", together with {count} container entries,"
    };
}
