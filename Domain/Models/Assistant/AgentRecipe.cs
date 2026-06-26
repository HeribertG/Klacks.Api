// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentRecipe : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public string TriggerJson { get; set; } = "{}";

    public string StepsJson { get; set; } = "[]";

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; }

    public int Version { get; set; } = 1;

    public Dictionary<string, List<string>>? Synonyms { get; set; }
}
