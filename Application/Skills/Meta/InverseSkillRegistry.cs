// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.Meta;

/// <summary>
/// Phase 5 — read-only map of "skill X is rolled back by skill Y with these param mappings".
/// Skills not in the map are NOT auto-reversible and must escalate to HITL.
///
/// Since 2026-09-16 the entries also carry a structured mapping (copied argument names, or the result
/// property holding a created id) so the graceful-correction path can build the inverse call itself
/// instead of describing it. The two halves are read through two accessors: TryBuildUndo (over TryGet)
/// sees every entry, while TryGetRollbackEntry hides the UndoOnly ones, so SkillRiskClassifier and
/// RollbackMyLastChangeSkill answer exactly as they did before the structured entries existed.
/// </summary>
public static class InverseSkillRegistry
{
    /// <summary>
    /// Marks an entry whose inverse is a human decision, not a callable skill. Public because three
    /// places compare against it - the classifier, the rollback skill and the undo builder - and a
    /// literal repeated three times is a magic string waiting to drift.
    /// </summary>
    public const string ManualMarker = "__manual__";

    /// <summary>
    /// Lookup: original-skill-name → (inverse-skill-name, param-mapping-hint).
    /// Param-mapping-hint is informational — the actual rollback skill must take the right id
    /// from the original execution's result.
    ///
    /// The structured entries at the end of the table are proposed 2026-09-16, owner approval pending
    /// (spec §8). They are all marked UndoOnly: none of those skills had an entry before, and each of
    /// them would otherwise change risk class and leave the autonomy gate, which
    /// SkillRiskReversibilityPinTests caught and which is not part of this change.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, InverseSkillEntry> Map =
        new Dictionary<string, InverseSkillEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["place_work"] = new("delete_work", "Take workId from the original execution's result."),
            ["add_break"] = new("delete_break", "Take breakId from the original execution's result."),
            ["add_break_placeholder"] = new("delete_break_placeholder", "Take the placeholder Id from the original execution's result."),
            ["update_break_placeholder"] = new("update_break_placeholder", "Re-apply the previous values reported in the original execution's result."),
            ["add_expense"] = new("delete_expense", "Take expenseId from the original execution's result."),
            ["confirm_work"] = new("unconfirm_work", "Same workId."),
            ["unconfirm_work"] = new("confirm_work", "Same workId."),
            ["approve_day"] = new("revoke_day_approval", "Same date + groupId."),
            ["revoke_day_approval"] = new("approve_day", "Same date + groupId."),
            ["close_period"] = new("reopen_period", "Same startDate + endDate."),
            ["reopen_period"] = new("close_period", "Same startDate + endDate."),
            ["accept_scenario"] = new(ManualMarker, "Accepting merges scenario into main; rollback is manual or requires a fresh scenario producer."),
            ["reject_scenario"] = new(ManualMarker, "Rejected scenarios cannot be revived — the data is soft-deleted."),
            ["add_client_to_group"] = new(ManualMarker, "remove_client_from_group skill TODO."),
            ["create_branch"] = new("delete_branch", "Take branchId from the original execution's result."),
            ["create_contract"] = new("delete_contract", "Take contractId from the original execution's result."),
            ["create_employee"] = new(ManualMarker, "delete_employee / mark inactive skill TODO."),
            ["create_user"] = new("delete_system_user", "Take userId."),
            ["create_shift"] = new(ManualMarker, "delete_shift skill TODO."),
            ["create_container_template"] = new(
                "delete_container_template",
                "Take containerId from the original execution's result. WARNING: the delete endpoint is " +
                "container-scoped, so it removes ALL weekday templates of that container, not only the one " +
                "just created. It is an exact undo only when the container had no template before — " +
                "otherwise list_container_template first and re-create the ones that must survive."),
            ["add_schedule_command"] = new(ManualMarker, "delete_schedule_command skill TODO."),
            ["start_autowizard"] = new("cancel_wizard_job", "Cancels in-flight jobs only; produced scenarios stay until accepted or rejected."),
            ["start_wizard1"] = new("cancel_wizard_job", "Cancels in-flight job."),
            ["start_wizard2"] = new("cancel_wizard_job", "Cancels in-flight job."),
            ["start_wizard3"] = new("cancel_wizard_job", "Cancels in-flight job."),
            ["add_ai_memory"] = new("delete_ai_memory", "Take memoryId."),
            ["install_language_pack"] = new("uninstall_language_pack", "Same language pack code."),
            ["uninstall_language_pack"] = new("install_language_pack", "Same language pack code; pack files remain on disk."),
            ["add_shift_to_group"] = new(
                "remove_shift_from_group", "Same shiftId and groupId.", ["shiftId", "groupId"],
                UndoOnly: true),
            ["add_container_template_task"] = new(
                "remove_container_template_task", "Same containerId, weekday and taskShiftId.",
                ["containerId", "weekday", "taskShiftId"], UndoOnly: true),
            ["create_group"] = new(
                "delete_group", "Take GroupId from the original execution's result.",
                null, "GroupId", "groupId", UndoOnly: true),
            ["create_calendar_selection"] = new(
                "delete_calendar_selection", "Take CalendarSelectionId from the original execution's result.",
                null, "CalendarSelectionId", "calendarSelectionId", UndoOnly: true),
            ["create_absence_type"] = new(
                "delete_absence_type", "Take AbsenceTypeId from the original execution's result.",
                null, "AbsenceTypeId", "absenceTypeId", UndoOnly: true)
        };

    public static bool TryGet(string skillName, out InverseSkillEntry inverse)
        => Map.TryGetValue(skillName, out inverse!);

    /// <summary>
    /// The prose view of the table: the entries that describe a rollback path a human may be told about.
    /// An UndoOnly entry is invisible here - it exists only so the correction path can build an undo
    /// call, and the skill must behave exactly as if it had no entry at all. Both readers of the prose
    /// half share this one rule: SkillRiskClassifier (is the skill reversible enough to run unattended?)
    /// and RollbackMyLastChangeSkill (what would a human call to undo it?). A __manual__ entry is
    /// deliberately still visible - it is a rollback path, just not a callable one, and each caller
    /// answers it its own way. The structured undo path keeps using TryGet, which sees every entry.
    /// </summary>
    /// <param name="skillName">The skill whose rollback path is asked for.</param>
    /// <param name="inverse">The entry, valid only when this returns true.</param>
    public static bool TryGetRollbackEntry(string skillName, out InverseSkillEntry inverse)
        => Map.TryGetValue(skillName, out inverse!) && !inverse.UndoOnly;

    /// <summary>
    /// Builds the exact inverse invocation for a call that was made, or reports that none can be built.
    /// False whenever anything is missing - the entry, an argument the inverse needs, or the created id -
    /// and always false for a __manual__ entry. Rule 3 says an undo is offered sparingly; silence is the
    /// correct answer to an incomplete mapping, never a half-filled call the user has to repair.
    /// </summary>
    /// <param name="skillName">The skill that was called and is to be undone.</param>
    /// <param name="argumentsJson">That call's arguments, as stored on the previous-action record.</param>
    /// <param name="resultDataJson">That call's result data, as stored on the previous-action record.</param>
    /// <param name="undo">The resolved inverse invocation, null when none could be built.</param>
    public static bool TryBuildUndo(
        string skillName, string? argumentsJson, string? resultDataJson, out SkillUndoInvocation? undo)
    {
        undo = null;
        if (!Map.TryGetValue(skillName, out var entry)
            || string.Equals(entry.SkillName, ManualMarker, StringComparison.Ordinal))
        {
            return false;
        }

        var arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in entry.CopiedArguments)
        {
            var value = ReadString(argumentsJson, name);
            if (value == null)
            {
                return false;
            }

            arguments[name] = value;
        }

        if (entry.ResultIdProperty != null)
        {
            var id = ReadString(resultDataJson, entry.ResultIdProperty);
            if (id == null || entry.ResultIdArgument == null)
            {
                return false;
            }

            arguments[entry.ResultIdArgument] = id;
        }

        if (arguments.Count == 0)
        {
            return false;
        }

        undo = new SkillUndoInvocation(entry.SkillName, arguments);
        return true;
    }

    /// <summary>
    /// One scalar property of a stored JSON object, or null. Only String, Number, True and False are
    /// values an argument can carry: a JSON null is an ABSENT value, and passing its raw text on would
    /// put the four letters of "null" into the call, while an object or an array would leak a raw JSON
    /// fragment. Both are a missing mapping, and a missing mapping means no undo at all (rule 3).
    /// </summary>
    /// <param name="json">The stored arguments or result data of the call being undone.</param>
    /// <param name="propertyName">The property to read, matched case-insensitively.</param>
    private static string? ReadString(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False =>
                        property.Value.GetRawText(),
                    _ => null
                };
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
