// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Root model for the skill-relation-seeds.json file containing experience-derived skill graph edges.
/// </summary>
/// <param name="Version">Schema version for migration compatibility</param>
/// <param name="Relations">List of skill relation seed definitions</param>
namespace Klacks.Api.Infrastructure.Persistence.Seed.Models;

public class SkillRelationSeedFile
{
    public int Version { get; set; }
    public List<SkillRelationSeedDefinition> Relations { get; set; } = new();
}
