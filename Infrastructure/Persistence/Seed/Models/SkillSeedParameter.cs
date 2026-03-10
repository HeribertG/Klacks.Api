// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parameter definition for a skill seed entry.
/// </summary>
/// <param name="Name">Parameter identifier</param>
/// <param name="Description">LLM-facing description</param>
/// <param name="Type">Data type (String, Integer, Boolean, Enum, etc.)</param>
namespace Klacks.Api.Infrastructure.Persistence.Seed.Models;

public class SkillSeedParameter
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "String";
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public List<string>? EnumValues { get; set; }
}
