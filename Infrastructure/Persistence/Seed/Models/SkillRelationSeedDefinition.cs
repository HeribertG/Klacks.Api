// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single experience-derived skill graph edge as defined in skill-relation-seeds.json.
/// </summary>
/// <param name="SkillAName">Name of the first skill of the edge</param>
/// <param name="SkillBName">Name of the second skill of the edge</param>
/// <param name="Type">Edge type (CoRequired or Sequential)</param>
/// <param name="Confidence">Learned confidence carried over as prior</param>
/// <param name="SupportCount">Positive evidence count backing the edge</param>
/// <param name="ContradictionCount">Negative evidence count backing the edge</param>
/// <param name="Provenance">Original provenance of the learned edge</param>
/// <param name="Status">Edge status (Candidate, Active or Retired)</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Persistence.Seed.Models;

public class SkillRelationSeedDefinition
{
    public string SkillAName { get; set; } = string.Empty;
    public string SkillBName { get; set; } = string.Empty;
    public SkillRelationType Type { get; set; }
    public double Confidence { get; set; }
    public int SupportCount { get; set; }
    public int ContradictionCount { get; set; }
    public string Provenance { get; set; } = string.Empty;
    public SkillRelationStatus Status { get; set; }
}
