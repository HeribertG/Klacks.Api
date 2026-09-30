// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Catalogue of skills that preview and apply through one name, toggled by a boolean "apply" parameter
/// that defaults to false, and whose apply=false path was verified in the implementation to write nothing
/// (no database row, no group, no job, no inbox entry, no self-API write; apply_grouping_plan only records
/// its in-memory preview). RepeatedWriteCallGuard never rejects a preview call of these skills as a repeat,
/// so a preview still runs after a refused apply=true in the same turn. The preview is still recorded, so a
/// later apply=true of the same skill in the same turn stays rejected and needs the user's next turn. The
/// apply value is read with the same reader as the skills (SkillParameterReader, nullable bool): absent,
/// null or unreadable counts as false exactly as in the skill, so guard and skill always agree. Not used by
/// ReadOnlyRecipeWriteGuard: after a completed recipe a preview stays rejected like any Mutate skill.
/// </summary>
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Domain.Constants;

public static class PreviewApplySkillCalls
{
    public const string ApplyParameter = "apply";

    public static readonly IReadOnlySet<string> Skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        GroupingSkillNames.Apply,
        "fill_group_by_criteria",
        "partition_clients_by_address",
        "bulk_add_shifts_to_group",
        "bulk_add_absence_for_group",
        "add_selected_clients_to_group",
        "group_ungrouped_by_city_name",
        "assign_orders_to_groups",
        "assign_shifts_to_city_groups",
        "seal_open_orders",
        "schedule_recurring_task",
    };

    /// <summary>
    /// True when the call targets a catalogued skill and does not ask it to apply.
    /// </summary>
    /// <param name="skillName">Name of the called skill.</param>
    /// <param name="parameters">Raw call arguments; the apply value may still be a JsonElement.</param>
    public static bool IsPreviewCall(string? skillName, Dictionary<string, object>? parameters)
    {
        if (string.IsNullOrEmpty(skillName) || !Skills.Contains(skillName))
        {
            return false;
        }

        var apply = parameters == null ? null : SkillParameterReader.Read<bool?>(parameters, ApplyParameter);
        return apply != true;
    }
}
