// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;

namespace Klacks.Api.Application.Services.Assistant;

/// <summary>
/// Entities announced in the entity-changed notification of a skill whose page reloads on an entity outside the
/// fixed skill-graph vocabulary of SkillEntityMap. EntityChangeNotifier consults this map first. Every container
/// template skill names ContainerTemplate (the container template page) and Shift (the container is a shift);
/// list_container_template is read-only and never notifies, it is listed so the container skills stay complete.
/// </summary>
public static class SkillRefreshEntityMap
{
    public const string ContainerTemplate = "ContainerTemplate";

    public const string Shift = "Shift";

    private static readonly string[] ContainerTemplateEntities = [ContainerTemplate, Shift];

    public static readonly IReadOnlyDictionary<string, string[]> Map = new Dictionary<string, string[]>
    {
        ["add_container_template_task"] = ContainerTemplateEntities,
        ["remove_container_template_task"] = ContainerTemplateEntities,
        ["create_container_template"] = ContainerTemplateEntities,
        ["delete_container_template"] = ContainerTemplateEntities,
        ["list_container_template"] = ContainerTemplateEntities,
        ["reset_container_day"] = ContainerTemplateEntities,
        ["bundle_nearby_timerange_shifts_into_container"] = ContainerTemplateEntities,
        ["optimize_container_route"] = ContainerTemplateEntities,
    };
}
