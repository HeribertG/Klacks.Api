// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Root model for the skill-seeds.json file containing all skill definitions.
/// </summary>
/// <param name="Version">Schema version for migration compatibility</param>
/// <param name="Skills">List of skill seed definitions</param>
namespace Klacks.Api.Infrastructure.Persistence.Seed.Models;

public class SkillSeedFile
{
    public int Version { get; set; }
    public List<SkillSeedDefinition> Skills { get; set; } = new();
}
