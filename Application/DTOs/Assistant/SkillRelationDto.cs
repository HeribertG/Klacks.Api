// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read model of a learned/derived skill-relationship edge for the "Klacksy noticed" insight view.
/// Enum fields are surfaced as strings; internal skill names are shown only in this admin-only view.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.DTOs.Assistant;

public class SkillRelationDto
{
    public Guid Id { get; set; }

    public string SkillAName { get; set; } = string.Empty;

    public string SkillBName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public int SupportCount { get; set; }

    public int ContradictionCount { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Provenance { get; set; } = string.Empty;

    public DateTime? LastReinforcedAt { get; set; }

    public static SkillRelationDto From(SkillRelation entity) => new()
    {
        Id = entity.Id,
        SkillAName = entity.SkillAName,
        SkillBName = entity.SkillBName,
        Type = entity.Type.ToString(),
        Confidence = entity.Confidence,
        SupportCount = entity.SupportCount,
        ContradictionCount = entity.ContradictionCount,
        Source = entity.Source.ToString(),
        Status = entity.Status.ToString(),
        Provenance = entity.Provenance,
        LastReinforcedAt = entity.LastReinforcedAt,
    };
}
