// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant;

/// <summary>
/// Entities announced in the entity-changed notification of a skill whose page reloads on an entity outside the
/// fixed skill-graph vocabulary of SkillEntityMap. EntityChangeNotifier consults this map first. Every container
/// template skill that writes names ContainerTemplate (the container template page, which also edits day overrides)
/// and Shift (the container is a shift); assign_container_week writes works of a container and names Work and Shift. Read-only skills never notify and are not listed.
/// </summary>
public static class SkillRefreshEntityMap
{
    private const string ContainerTemplate = "ContainerTemplate";

    private const string Shift = "Shift";

    private static readonly string[] ContainerTemplateEntities = [ContainerTemplate, Shift];

    private static readonly string[] ContainerWeekBookingEntities = ["Work", Shift];

    public static readonly IReadOnlyDictionary<string, string[]> Map = new Dictionary<string, string[]>
    {
        ["add_container_template_task"] = ContainerTemplateEntities,
        ["add_container_template_break"] = ContainerTemplateEntities,
        ["remove_container_template_task"] = ContainerTemplateEntities,
        ["create_container_template"] = ContainerTemplateEntities,
        ["delete_container_template"] = ContainerTemplateEntities,
        ["reset_container_day"] = ContainerTemplateEntities,
        ["bundle_nearby_timerange_shifts_into_container"] = ContainerTemplateEntities,
        ["build_weekly_tours"] = ContainerTemplateEntities,
        ["assign_container_week"] = ContainerWeekBookingEntities,
        ["optimize_container_route"] = ContainerTemplateEntities,
    };
}
