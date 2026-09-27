// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One gate-passed description proposal as the export script needs it.
/// </summary>
/// <param name="ProposalId">Proposal to mark as exported afterwards</param>
/// <param name="SkillId">Skill the proposal changes</param>
/// <param name="SkillName">Name the export script looks up in skill-seeds.json</param>
/// <param name="ValueBefore">Description the gate measured against; the script only patches a seed that still has it</param>
/// <param name="ValueAfter">Description the gate passed</param>
/// <param name="DbVersion">Version of the skill row, null when the skill no longer exists</param>
/// <param name="DbSeedVersion">Seed version of the skill row</param>
/// <param name="DbDescription">Description currently live in this database</param>
/// <param name="GateMetricsJson">The gate metrics as stored</param>
/// <param name="GatePassedAtUtc">When the gate decided</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record LearnedDescriptionExportCandidateDto(
    Guid ProposalId,
    Guid SkillId,
    string SkillName,
    string ValueBefore,
    string ValueAfter,
    int? DbVersion,
    int? DbSeedVersion,
    string? DbDescription,
    string? GateMetricsJson,
    DateTime? GatePassedAtUtc);
